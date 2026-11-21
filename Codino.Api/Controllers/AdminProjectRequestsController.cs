
using Codino.Api.Data;
using Codino.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Codino.Api.Controllers
{
    // تمام عملیات این کنترلر فقط برای مدیر سایت مجاز است.
    [ApiController]
    [Route("api/admin/project-requests")]
    [Authorize(Roles = "Admin")]
    public class AdminProjectRequestsController : ControllerBase
    {
        private static readonly HashSet<string> AllowedStatuses =
            new(StringComparer.Ordinal)
            {
                "Pending",
                "Approved",
                "Rejected"
            };

        private readonly CodinoDbContext _context;
        private readonly ILogger<AdminProjectRequestsController> _logger;

        public AdminProjectRequestsController(
            CodinoDbContext context,
            ILogger<AdminProjectRequestsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // =====================================================
        // GET ALL PROJECT REQUESTS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            if (page < 1)
                page = 1;

            pageSize = Math.Clamp(pageSize, 1, 100);

            if (!string.IsNullOrWhiteSpace(status))
            {
                status = status.Trim();

                if (!AllowedStatuses.Contains(status))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "وضعیت انتخاب‌شده معتبر نیست."
                    });
                }
            }

            var query = _context.ProjectRequests
                .AsNoTracking()
                .AsQueryable();

            // فیلتر وضعیت
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(x => x.Status == status);
            }

            // جست‌وجو در اطلاعات اصلی درخواست
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(x =>
                    x.CustomerName.Contains(search) ||
                    x.PhoneNumber.Contains(search) ||
                    x.ProjectName.Contains(search) ||
                    x.Email.Contains(search));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var requests = await query
                .OrderByDescending(x => x.CreatedAtUtc)
                .ThenByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new
                {
                    x.Id,
                    x.UserId,
                    x.CustomerName,
                    x.Email,
                    x.PhoneNumber,
                    x.ProjectName,
                    x.ProjectType,
                    x.Budget,
                    x.DesiredDeadline,
                    x.Status,
                    x.CreatedAtUtc
                })
                .ToListAsync(cancellationToken);

            return Ok(new
            {
                success = true,
                data = requests,
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
        // GET PROJECT REQUEST DETAILS
        // =====================================================

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(
            int id,
            CancellationToken cancellationToken)
        {
            if (id <= 0)
                return BadRequest(new
                {
                    success = false,
                    message = "شناسه درخواست معتبر نیست."
                });

            var request = await _context.ProjectRequests
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new
                {
                    x.Id,
                    x.UserId,
                    x.CustomerName,
                    x.Email,
                    x.PhoneNumber,
                    x.ProjectName,
                    x.ProjectType,
                    x.Description,
                    x.Budget,
                    x.DesiredDeadline,
                    x.Status,
                    x.CreatedAtUtc
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (request == null)
                return NotFound(new
                {
                    success = false,
                    message = "درخواست موردنظر پیدا نشد."
                });

            return Ok(new
            {
                success = true,
                data = request
            });
        }

        // =====================================================
        // UPDATE PROJECT REQUEST STATUS
        // =====================================================

        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(
            int id,
            [FromBody] UpdateProjectRequestStatusDto? request,
            CancellationToken cancellationToken)
        {
            if (id <= 0 || request == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "اطلاعات ارسالی معتبر نیست."
                });
            }

            var newStatus = request.Status?.Trim();

            if (string.IsNullOrWhiteSpace(newStatus) ||
                !AllowedStatuses.Contains(newStatus))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "وضعیت انتخاب‌شده معتبر نیست."
                });
            }

            // فقط در صورتی تغییر انجام می‌شود که وضعیت فعلی متفاوت باشد.
            var affectedRows = await _context.ProjectRequests
                .Where(x => x.Id == id && x.Status != newStatus)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        x => x.Status,
                        newStatus),
                    cancellationToken);

            if (affectedRows == 0)
            {
                var exists = await _context.ProjectRequests
                    .AnyAsync(x => x.Id == id, cancellationToken);

                if (!exists)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "درخواست موردنظر پیدا نشد."
                    });
                }
            }

            _logger.LogInformation(
                "Project request {RequestId} status updated to {Status} by admin.",
                id,
                newStatus);

            return Ok(new
            {
                success = true,
                message = "وضعیت درخواست با موفقیت به‌روزرسانی شد.",
                id,
                status = newStatus
            });
        }

        // =====================================================
        // DELETE PROJECT REQUEST
        // =====================================================

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(
            int id,
            CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "شناسه درخواست معتبر نیست."
                });
            }

            var affectedRows = await _context.ProjectRequests
                .Where(x => x.Id == id)
                .ExecuteDeleteAsync(cancellationToken);

            if (affectedRows == 0)
            {
                return NotFound(new
                {
                    success = false,
                    message = "درخواست موردنظر پیدا نشد."
                });
            }

            _logger.LogInformation(
                "Project request {RequestId} deleted by admin.",
                id);

            return Ok(new
            {
                success = true,
                message = "درخواست پروژه با موفقیت حذف شد."
            });
        }
    }

    // =========================================================
    // UPDATE STATUS DTO
    // =========================================================

    public class UpdateProjectRequestStatusDto
    {
        public string? Status { get; set; }
    }
}
