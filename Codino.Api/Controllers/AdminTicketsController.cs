using Codino.Api.Data;
using Codino.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Codino.Api.Controllers
{
    [ApiController]
    [Route("api/admin/tickets")]
    [Authorize(Roles = "Admin")]
    public class AdminTicketsController : ControllerBase
    {
        private static readonly HashSet<string> AllowedStatuses =
            new(StringComparer.Ordinal)
            {
                "Open",
                "WaitingForAdmin",
                "WaitingForUser",
                "Closed"
            };

        private static readonly HashSet<string> AllowedPriorities =
            new(StringComparer.Ordinal)
            {
                "Low",
                "Normal",
                "High"
            };

        private static readonly HashSet<string> AllowedCategories =
            new(StringComparer.Ordinal)
            {
                "General",
                "Technical",
                "Billing",
                "Account",
                "Other"
            };

        private readonly CodinoDbContext _context;
        private readonly ILogger<AdminTicketsController> _logger;

        public AdminTicketsController(
            CodinoDbContext context,
            ILogger<AdminTicketsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // =====================================================
        // GET ADMIN USER ID
        // =====================================================

        private bool TryGetAdminId(out int adminId)
        {
            var claim = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            return int.TryParse(claim, out adminId);
        }

        // =====================================================
        // GET ALL TICKETS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] string? priority,
            [FromQuery] string? category,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            if (!string.IsNullOrWhiteSpace(status))
            {
                status = status.Trim();

                if (!AllowedStatuses.Contains(status))
                    return BadRequest(new
                    {
                        success = false,
                        message = "وضعیت انتخاب‌شده معتبر نیست."
                    });
            }

            if (!string.IsNullOrWhiteSpace(priority))
            {
                priority = priority.Trim();

                if (!AllowedPriorities.Contains(priority))
                    return BadRequest(new
                    {
                        success = false,
                        message = "اولویت انتخاب‌شده معتبر نیست."
                    });
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                category = category.Trim();

                if (!AllowedCategories.Contains(category))
                    return BadRequest(new
                    {
                        success = false,
                        message = "دسته‌بندی انتخاب‌شده معتبر نیست."
                    });
            }

            var query =
                from ticket in _context.Tickets.AsNoTracking()
                join user in _context.Users.AsNoTracking()
                    on ticket.UserId equals user.Id
                select new
                {
                    Ticket = ticket,
                    UserName = user.FullName,
                    PhoneNumber = user.PhoneNumber
                };

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(x =>
                    x.Ticket.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(priority))
            {
                query = query.Where(x =>
                    x.Ticket.Priority == priority);
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(x =>
                    x.Ticket.Category == category);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(x =>
                    x.Ticket.TicketNumber.Contains(search) ||
                    x.Ticket.Subject.Contains(search) ||
                    x.UserName.Contains(search) ||
                    x.PhoneNumber.Contains(search));
            }

            var totalCount =
                await query.CountAsync(cancellationToken);

            var tickets = await query
                .OrderByDescending(x =>
                    x.Ticket.LastMessageAtUtc ??
                    x.Ticket.CreatedAtUtc)
                .ThenByDescending(x => x.Ticket.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new
                {
                    x.Ticket.Id,
                    x.Ticket.TicketNumber,
                    x.Ticket.Subject,
                    x.Ticket.Category,
                    x.Ticket.Priority,
                    x.Ticket.Status,
                    x.Ticket.CreatedAtUtc,
                    x.Ticket.LastMessageAtUtc,

                    CustomerName = x.UserName,
                    CustomerPhone = x.PhoneNumber,

                    MessageCount = _context.TicketMessages
                        .Count(m =>
                            m.TicketId == x.Ticket.Id)
                })
                .ToListAsync(cancellationToken);

            return Ok(new
            {
                success = true,
                data = tickets,
                pagination = new
                {
                    page,
                    pageSize,
                    totalCount,
                    totalPages = (int)Math.Ceiling(
                        totalCount / (double)pageSize)
                }
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
            if (id <= 0)
                return BadRequest(new
                {
                    success = false,
                    message = "شناسه تیکت معتبر نیست."
                });

            var ticket = await (
                from t in _context.Tickets.AsNoTracking()
                join u in _context.Users.AsNoTracking()
                    on t.UserId equals u.Id
                where t.Id == id
                select new
                {
                    t.Id,
                    t.TicketNumber,
                    t.Subject,
                    t.Category,
                    t.Priority,
                    t.Status,
                    t.CreatedAtUtc,
                    t.UpdatedAtUtc,
                    t.LastMessageAtUtc,

                    Customer = new
                    {
                        u.Id,
                        u.FullName,
                        u.PhoneNumber
                    },

                    Messages = _context.TicketMessages
                        .Where(m => m.TicketId == t.Id)
                        .OrderBy(m => m.CreatedAtUtc)
                        .ThenBy(m => m.Id)
                        .Select(m => new
                        {
                            m.Id,
                            m.SenderUserId,
                            m.IsAdmin,
                            m.Message,
                            m.CreatedAtUtc
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (ticket == null)
                return NotFound(new
                {
                    success = false,
                    message = "تیکت موردنظر پیدا نشد."
                });

            return Ok(new
            {
                success = true,
                data = ticket
            });
        }

        // =====================================================
        // ADMIN REPLY
        // =====================================================

        [HttpPost("{id:int}/reply")]
        public async Task<IActionResult> Reply(
            int id,
            [FromBody] AdminTicketReplyDto? request,
            CancellationToken cancellationToken)
        {
            if (id <= 0 || request == null)
                return BadRequest(new
                {
                    success = false,
                    message = "اطلاعات ارسالی معتبر نیست."
                });

            if (!TryGetAdminId(out var adminId))
                return Unauthorized(new
                {
                    success = false,
                    message = "شناسه مدیر معتبر نیست."
                });

            var message = request.Message?.Trim() ?? "";

            if (message.Length < 1 || message.Length > 5000)
                return BadRequest(new
                {
                    success = false,
                    message = "متن پاسخ باید بین ۱ تا ۵۰۰۰ کاراکتر باشد."
                });

            var ticket = await _context.Tickets
                .FirstOrDefaultAsync(
                    x => x.Id == id,
                    cancellationToken);

            if (ticket == null)
                return NotFound(new
                {
                    success = false,
                    message = "تیکت موردنظر پیدا نشد."
                });

            if (ticket.Status == "Closed")
                return BadRequest(new
                {
                    success = false,
                    message = "تیکت بسته شده و امکان پاسخ‌گویی ندارد."
                });

            var now = DateTime.UtcNow;

            var ticketMessage = new TicketMessage
            {
                TicketId = ticket.Id,
                SenderUserId = adminId,
                IsAdmin = true,
                Message = message,
                CreatedAtUtc = now
            };

            ticket.Status = "WaitingForUser";
            ticket.UpdatedAtUtc = now;
            ticket.LastMessageAtUtc = now;

            _context.TicketMessages.Add(ticketMessage);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Admin {AdminId} replied to ticket {TicketId}.",
                adminId,
                id);

            return Ok(new
            {
                success = true,
                message = "پاسخ شما با موفقیت ارسال شد.",
                data = new
                {
                    ticketMessage.Id,
                    ticketMessage.TicketId,
                    ticketMessage.SenderUserId,
                    ticketMessage.IsAdmin,
                    ticketMessage.Message,
                    ticketMessage.CreatedAtUtc,
                    ticket.Status
                }
            });
        }

        // =====================================================
        // UPDATE TICKET STATUS
        // =====================================================

        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(
            int id,
            [FromBody] UpdateTicketStatusDto? request,
            CancellationToken cancellationToken)
        {
            if (id <= 0 || request == null)
                return BadRequest(new
                {
                    success = false,
                    message = "اطلاعات ارسالی معتبر نیست."
                });

            var newStatus = request.Status?.Trim();

            if (string.IsNullOrWhiteSpace(newStatus) ||
                !AllowedStatuses.Contains(newStatus))
                return BadRequest(new
                {
                    success = false,
                    message = "وضعیت انتخاب‌شده معتبر نیست."
                });

            var ticket = await _context.Tickets
                .FirstOrDefaultAsync(
                    x => x.Id == id,
                    cancellationToken);

            if (ticket == null)
                return NotFound(new
                {
                    success = false,
                    message = "تیکت موردنظر پیدا نشد."
                });

            ticket.Status = newStatus;
            ticket.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Ticket {TicketId} status changed to {Status}.",
                id,
                newStatus);

            return Ok(new
            {
                success = true,
                message = "وضعیت تیکت با موفقیت تغییر کرد.",
                id = ticket.Id,
                status = ticket.Status
            });
        }

        // =====================================================
        // UPDATE TICKET PRIORITY
        // =====================================================

        [HttpPatch("{id:int}/priority")]
        public async Task<IActionResult> UpdatePriority(
            int id,
            [FromBody] UpdateTicketPriorityDto? request,
            CancellationToken cancellationToken)
        {
            if (id <= 0 || request == null)
                return BadRequest(new
                {
                    success = false,
                    message = "اطلاعات ارسالی معتبر نیست."
                });

            var newPriority = request.Priority?.Trim();

            if (string.IsNullOrWhiteSpace(newPriority) ||
                !AllowedPriorities.Contains(newPriority))
                return BadRequest(new
                {
                    success = false,
                    message = "اولویت انتخاب‌شده معتبر نیست."
                });

            var ticket = await _context.Tickets
                .FirstOrDefaultAsync(
                    x => x.Id == id,
                    cancellationToken);

            if (ticket == null)
                return NotFound(new
                {
                    success = false,
                    message = "تیکت موردنظر پیدا نشد."
                });

            ticket.Priority = newPriority;
            ticket.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(new
            {
                success = true,
                message = "اولویت تیکت تغییر کرد.",
                id = ticket.Id,
                priority = ticket.Priority
            });
        }

        // =====================================================
        // DELETE TICKET
        // =====================================================

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(
            int id,
            CancellationToken cancellationToken)
        {
            if (id <= 0)
                return BadRequest(new
                {
                    success = false,
                    message = "شناسه تیکت معتبر نیست."
                });

            var ticket = await _context.Tickets
                .FirstOrDefaultAsync(
                    x => x.Id == id,
                    cancellationToken);

            if (ticket == null)
                return NotFound(new
                {
                    success = false,
                    message = "تیکت موردنظر پیدا نشد."
                });

            _context.Tickets.Remove(ticket);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Ticket {TicketId} deleted by admin.",
                id);

            return Ok(new
            {
                success = true,
                message = "تیکت با موفقیت حذف شد."
            });
        }
    }

    // =========================================================
    // ADMIN REPLY DTO
    // =========================================================

    public class AdminTicketReplyDto
    {
        public string? Message { get; set; }
    }

    // =========================================================
    // UPDATE TICKET STATUS DTO
    // =========================================================

    public class UpdateTicketStatusDto
    {
        public string? Status { get; set; }
    }

    // =========================================================
    // UPDATE TICKET PRIORITY DTO
    // =========================================================

    public class UpdateTicketPriorityDto
    {
        public string? Priority { get; set; }
    }
}
