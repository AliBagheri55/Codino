
using Codino.Api.Data;
using Codino.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Codino.Api.Controllers
{
    [ApiController]
    [Route("api/admin/announcements")]
    [Authorize(Roles = "Admin")]
    public class AdminAnnouncementsController : ControllerBase
    {
        private readonly CodinoDbContext _context;
        private readonly ILogger<AdminAnnouncementsController> _logger;

        public AdminAnnouncementsController(
            CodinoDbContext context,
            ILogger<AdminAnnouncementsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // =========================================
        // GET ALL ANNOUNCEMENTS
        // =========================================

        [HttpGet]
        public async Task<IActionResult> GetAll(
            CancellationToken cancellationToken)
        {
            var announcements = await _context.Announcements
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAtUtc)
                .ThenByDescending(x => x.Id)
                .Select(x => new
                {
                    x.Id,
                    x.Title,
                    x.Content,
                    x.AuthorLabel,
                    x.IsPublished,
                    x.CreatedAtUtc,
                    x.UpdatedAtUtc
                })
                .ToListAsync(cancellationToken);

            return Ok(new
            {
                success = true,
                data = announcements
            });
        }

        // =========================================
        // GET ANNOUNCEMENT DETAILS
        // =========================================

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetDetails(
            int id,
            CancellationToken cancellationToken)
        {
            if (id <= 0)
                return BadRequest(new
                {
                    success = false,
                    message = "شناسه اطلاعیه معتبر نیست."
                });

            var announcement = await _context.Announcements
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new
                {
                    x.Id,
                    x.Title,
                    x.Content,
                    x.AuthorLabel,
                    x.IsPublished,
                    x.CreatedAtUtc,
                    x.UpdatedAtUtc
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (announcement == null)
                return NotFound(new
                {
                    success = false,
                    message = "اطلاعیه موردنظر پیدا نشد."
                });

            return Ok(new
            {
                success = true,
                data = announcement
            });
        }

        // =========================================
        // CREATE ANNOUNCEMENT
        // =========================================

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] AnnouncementRequestDto? request,
            CancellationToken cancellationToken)
        {
            if (request == null)
                return BadRequest(new
                {
                    success = false,
                    message = "اطلاعات ارسالی معتبر نیست."
                });

            var title = request.Title?.Trim() ?? "";
            var content = request.Content?.Trim() ?? "";
            var authorLabel = request.AuthorLabel?.Trim();

            if (title.Length < 3 || title.Length > 150)
                return BadRequest(new
                {
                    success = false,
                    message = "عنوان باید بین ۳ تا ۱۵۰ کاراکتر باشد."
                });

            if (content.Length < 1 || content.Length > 10000)
                return BadRequest(new
                {
                    success = false,
                    message = "متن اطلاعیه باید بین ۱ تا ۱۰۰۰۰ کاراکتر باشد."
                });

            if (string.IsNullOrWhiteSpace(authorLabel))
                authorLabel = "واحد پشتیبانی | Codino";

            if (authorLabel.Length > 100)
                return BadRequest(new
                {
                    success = false,
                    message = "نام واحد منتشرکننده حداکثر ۱۰۰ کاراکتر است."
                });

            var now = DateTime.UtcNow;

            var announcement = new Announcement
            {
                Title = title,
                Content = content,
                AuthorLabel = authorLabel,
                IsPublished = request.IsPublished,
                CreatedAtUtc = now
            };

            _context.Announcements.Add(announcement);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Announcement {AnnouncementId} created.",
                announcement.Id);

            return Ok(new
            {
                success = true,
                message = "اطلاعیه با موفقیت ایجاد شد.",
                data = new
                {
                    announcement.Id,
                    announcement.Title,
                    announcement.Content,
                    announcement.AuthorLabel,
                    announcement.IsPublished,
                    announcement.CreatedAtUtc
                }
            });
        }

        // =========================================
        // UPDATE ANNOUNCEMENT
        // =========================================

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] AnnouncementRequestDto? request,
            CancellationToken cancellationToken)
        {
            if (id <= 0 || request == null)
                return BadRequest(new
                {
                    success = false,
                    message = "اطلاعات ارسالی معتبر نیست."
                });

            var title = request.Title?.Trim() ?? "";
            var content = request.Content?.Trim() ?? "";
            var authorLabel = request.AuthorLabel?.Trim();

            if (title.Length < 3 || title.Length > 150)
                return BadRequest(new
                {
                    success = false,
                    message = "عنوان باید بین ۳ تا ۱۵۰ کاراکتر باشد."
                });

            if (content.Length < 1 || content.Length > 10000)
                return BadRequest(new
                {
                    success = false,
                    message = "متن اطلاعیه باید بین ۱ تا ۱۰۰۰۰ کاراکتر باشد."
                });

            if (string.IsNullOrWhiteSpace(authorLabel))
                authorLabel = "واحد پشتیبانی | Codino";

            if (authorLabel.Length > 100)
                return BadRequest(new
                {
                    success = false,
                    message = "نام واحد منتشرکننده حداکثر ۱۰۰ کاراکتر است."
                });

            var announcement = await _context.Announcements
                .FirstOrDefaultAsync(
                    x => x.Id == id,
                    cancellationToken);

            if (announcement == null)
                return NotFound(new
                {
                    success = false,
                    message = "اطلاعیه موردنظر پیدا نشد."
                });

            announcement.Title = title;
            announcement.Content = content;
            announcement.AuthorLabel = authorLabel;
            announcement.IsPublished = request.IsPublished;
            announcement.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Announcement {AnnouncementId} updated.",
                id);

            return Ok(new
            {
                success = true,
                message = "اطلاعیه با موفقیت ویرایش شد."
            });
        }

        // =========================================
        // CHANGE PUBLISH STATUS
        // =========================================

        [HttpPatch("{id:int}/publish")]
        public async Task<IActionResult> ChangePublishStatus(
            int id,
            [FromBody] AnnouncementPublishDto? request,
            CancellationToken cancellationToken)
        {
            if (id <= 0 || request == null)
                return BadRequest(new
                {
                    success = false,
                    message = "اطلاعات ارسالی معتبر نیست."
                });

            var announcement = await _context.Announcements
                .FirstOrDefaultAsync(
                    x => x.Id == id,
                    cancellationToken);

            if (announcement == null)
                return NotFound(new
                {
                    success = false,
                    message = "اطلاعیه موردنظر پیدا نشد."
                });

            announcement.IsPublished = request.IsPublished;
            announcement.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(new
            {
                success = true,
                message = request.IsPublished
                    ? "اطلاعیه منتشر شد."
                    : "اطلاعیه از انتشار خارج شد.",
                id = announcement.Id,
                isPublished = announcement.IsPublished
            });
        }

        // =========================================
        // DELETE ANNOUNCEMENT
        // =========================================

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(
            int id,
            CancellationToken cancellationToken)
        {
            if (id <= 0)
                return BadRequest(new
                {
                    success = false,
                    message = "شناسه اطلاعیه معتبر نیست."
                });

            var announcement = await _context.Announcements
                .FirstOrDefaultAsync(
                    x => x.Id == id,
                    cancellationToken);

            if (announcement == null)
                return NotFound(new
                {
                    success = false,
                    message = "اطلاعیه موردنظر پیدا نشد."
                });

            _context.Announcements.Remove(announcement);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Announcement {AnnouncementId} deleted.",
                id);

            return Ok(new
            {
                success = true,
                message = "اطلاعیه با موفقیت حذف شد."
            });
        }
    }

    // =============================================
    // ANNOUNCEMENT REQUEST DTO
    // =============================================

    public class AnnouncementRequestDto
    {
        public string? Title { get; set; }

        public string? Content { get; set; }

        public string? AuthorLabel { get; set; }

        public bool IsPublished { get; set; } = true;
    }

    // =============================================
    // ANNOUNCEMENT PUBLISH DTO
    // =============================================

    public class AnnouncementPublishDto
    {
        public bool IsPublished { get; set; }
    }
}
