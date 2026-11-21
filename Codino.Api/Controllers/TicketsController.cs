
using Codino.Api.Data;
using Codino.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Codino.Api.Controllers
{
    [ApiController]
    [Route("api/tickets")]
    [Authorize]
    public class TicketsController : ControllerBase
    {
        private readonly CodinoDbContext _context;
        private readonly ILogger<TicketsController> _logger;

        private static readonly HashSet<string> AllowedCategories =
            new(StringComparer.Ordinal)
            {
                "General",
                "Technical",
                "Billing",
                "Account",
                "Other"
            };

        private static readonly HashSet<string> AllowedPriorities =
            new(StringComparer.Ordinal)
            {
                "Low",
                "Normal",
                "High"
            };

        public TicketsController(
            CodinoDbContext context,
            ILogger<TicketsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // =====================================================
        // GET AUTHENTICATED USER ID
        // =====================================================

        private bool TryGetUserId(out int userId)
        {
            var claim = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            return int.TryParse(claim, out userId);
        }

        // =====================================================
        // CREATE TICKET
        // =====================================================

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateTicketDto? request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "اطلاعات تیکت ارسال نشده است."
                });
            }

            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "اطلاعات حساب کاربری معتبر نیست."
                });
            }

            var subject = request.Subject?.Trim() ?? "";
            var message = request.Message?.Trim() ?? "";
            var category = request.Category?.Trim() ?? "";
            var priority = request.Priority?.Trim() ?? "";

            if (subject.Length < 3 || subject.Length > 150)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "عنوان تیکت باید بین ۳ تا ۱۵۰ کاراکتر باشد."
                });
            }

            if (message.Length < 10 || message.Length > 5000)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "متن پیام باید بین ۱۰ تا ۵۰۰۰ کاراکتر باشد."
                });
            }

            if (!AllowedCategories.Contains(category))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "دسته‌بندی انتخاب‌شده معتبر نیست."
                });
            }

            if (!AllowedPriorities.Contains(priority))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "اولویت انتخاب‌شده معتبر نیست."
                });
            }

            var now = DateTime.UtcNow;

            // شماره اختصاصی تیکت
            var ticketNumber =
                $"CD-{now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

            var ticket = new Ticket
            {
                TicketNumber = ticketNumber,
                UserId = userId,
                Subject = subject,
                Category = category,
                Priority = priority,
                Status = "Open",
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                LastMessageAtUtc = now
            };

            var firstMessage = new TicketMessage
            {
                Ticket = ticket,
                SenderUserId = userId,
                IsAdmin = false,
                Message = message,
                CreatedAtUtc = now
            };

            try
            {
                _context.Tickets.Add(ticket);
                _context.TicketMessages.Add(firstMessage);

                await _context.SaveChangesAsync(cancellationToken);

                return StatusCode(
                    StatusCodes.Status201Created,
                    new
                    {
                        success = true,
                        message = "تیکت شما با موفقیت ثبت شد.",
                        data = new
                        {
                            ticket.Id,
                            ticket.TicketNumber,
                            ticket.Subject,
                            ticket.Category,
                            ticket.Priority,
                            ticket.Status,
                            ticket.CreatedAtUtc
                        }
                    });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Error creating ticket for user {UserId}.",
                    userId);

                return StatusCode(500, new
                {
                    success = false,
                    message = "ثبت تیکت انجام نشد. لطفاً دوباره تلاش کنید."
                });
            }
        }

        // =====================================================
        // GET MY TICKETS
        // =====================================================

        [HttpGet("my-tickets")]
        public async Task<IActionResult> GetMyTickets(
            CancellationToken cancellationToken)
        {
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "اطلاعات حساب کاربری معتبر نیست."
                });
            }

            var tickets = await _context.Tickets
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.LastMessageAtUtc ?? x.CreatedAtUtc)
                .Take(100)
                .Select(x => new
                {
                    x.Id,
                    x.TicketNumber,
                    x.Subject,
                    x.Category,
                    x.Priority,
                    x.Status,
                    x.CreatedAtUtc,
                    x.UpdatedAtUtc,
                    x.LastMessageAtUtc,
                    MessageCount = _context.TicketMessages
                        .Count(m => m.TicketId == x.Id)
                })
                .ToListAsync(cancellationToken);

            return Ok(new
            {
                success = true,
                data = tickets
            });
        }

        // =====================================================
        // GET TICKET DETAILS
        // =====================================================

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetDetails(
            int id,
            CancellationToken cancellationToken)
        {
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "اطلاعات حساب کاربری معتبر نیست."
                });
            }

            var ticket = await _context.Tickets
                .AsNoTracking()
                .Where(x => x.Id == id && x.UserId == userId)
                .Select(x => new
                {
                    x.Id,
                    x.TicketNumber,
                    x.Subject,
                    x.Category,
                    x.Priority,
                    x.Status,
                    x.CreatedAtUtc,
                    x.UpdatedAtUtc,
                    x.LastMessageAtUtc,

                    Messages = _context.TicketMessages
                        .Where(m => m.TicketId == x.Id)
                        .OrderBy(m => m.CreatedAtUtc)
                        .Select(m => new
                        {
                            m.Id,
                            m.Message,
                            m.IsAdmin,
                            m.CreatedAtUtc
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (ticket == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "تیکت موردنظر پیدا نشد."
                });
            }

            return Ok(new
            {
                success = true,
                data = ticket
            });
        }

        // =====================================================
        // SEND MESSAGE
        // =====================================================

        [HttpPost("{id:int}/messages")]
        public async Task<IActionResult> SendMessage(
            int id,
            [FromBody] SendTicketMessageDto? request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "متن پیام ارسال نشده است."
                });
            }

            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "اطلاعات حساب کاربری معتبر نیست."
                });
            }

            var message = request.Message?.Trim() ?? "";

            if (message.Length < 1 || message.Length > 5000)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "متن پیام باید بین ۱ تا ۵۰۰۰ کاراکتر باشد."
                });
            }

            var ticket = await _context.Tickets
                .FirstOrDefaultAsync(
                    x => x.Id == id && x.UserId == userId,
                    cancellationToken);

            if (ticket == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "تیکت موردنظر پیدا نشد."
                });
            }

            if (ticket.Status == "Closed")
            {
                return BadRequest(new
                {
                    success = false,
                    message = "این تیکت بسته شده و امکان ارسال پیام ندارد."
                });
            }

            var now = DateTime.UtcNow;

            var ticketMessage = new TicketMessage
            {
                TicketId = ticket.Id,
                SenderUserId = userId,
                IsAdmin = false,
                Message = message,
                CreatedAtUtc = now
            };

            ticket.Status = "WaitingForAdmin";
            ticket.UpdatedAtUtc = now;
            ticket.LastMessageAtUtc = now;

            _context.TicketMessages.Add(ticketMessage);

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(new
            {
                success = true,
                message = "پیام شما با موفقیت ارسال شد.",
                data = new
                {
                    ticketMessage.Id,
                    ticketMessage.Message,
                    ticketMessage.CreatedAtUtc,
                    ticket.Status
                }
            });
        }
    }

    // =========================================================
    // CREATE TICKET DTO
    // =========================================================

    public class CreateTicketDto
    {
        [Required]
        [StringLength(150, MinimumLength = 3)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Category { get; set; } = "General";

        [Required]
        [StringLength(20)]
        public string Priority { get; set; } = "Normal";

        [Required]
        [StringLength(5000, MinimumLength = 10)]
        public string Message { get; set; } = string.Empty;
    }

    // =========================================================
    // SEND TICKET MESSAGE DTO
    // =========================================================

    public class SendTicketMessageDto
    {
        [Required]
        [StringLength(5000, MinimumLength = 1)]
        public string Message { get; set; } = string.Empty;
    }
}
