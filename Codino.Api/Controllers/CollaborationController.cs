
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
    [Route("api/[controller]")]
    [Authorize]
    public class CollaborationController : ControllerBase
    {
        private readonly CodinoDbContext _context;
        private readonly ILogger<CollaborationController> _logger;

        public CollaborationController(
            CodinoDbContext context,
            ILogger<CollaborationController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("submit")]
        public async Task<IActionResult> Submit(
            [FromBody] CreateCollaborationRequest request)
        {
            try
            {
                // دریافت شناسه کاربر از اطلاعات احراز هویت
                var userIdValue = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                if (!int.TryParse(userIdValue, out var userId))
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "اطلاعات کاربری معتبر نیست."
                    });
                }

                // بررسی وجود کاربر
                var user = await _context.Users
                    .FirstOrDefaultAsync(x => x.Id == userId);

                if (user == null || !user.IsPhoneVerified)
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "ابتدا باید وارد حساب کاربری تأییدشده شوید."
                    });
                }

                // جلوگیری از ثبت درخواست تکراری در حالت انتظار
                var existingRequest = await _context.CollaborationRequests
                    .AnyAsync(x =>
                        x.UserId == userId &&
                        x.Status == "Pending");

                if (existingRequest)
                {
                    return Conflict(new
                    {
                        success = false,
                        message = "شما قبلاً درخواست همکاری ثبت کرده‌اید."
                    });
                }

                var collaboration = new CollaborationRequest
                {
                    UserId = userId,
                    Specialty = request.Specialty.Trim(),
                    Experience = request.Experience.Trim(),
                    Skills = request.Skills.Trim(),
                    PortfolioUrl = request.PortfolioUrl?.Trim(),
                    Description = request.Description.Trim(),
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                _context.CollaborationRequests.Add(collaboration);

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Collaboration request {RequestId} registered for user {UserId}",
                    collaboration.Id,
                    userId);

                return Ok(new
                {
                    success = true,
                    message = "درخواست همکاری شما با موفقیت ثبت شد. به‌زودی کارشناس کدینو با شما ارتباط خواهد گرفت.",
                    requestId = collaboration.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error registering collaboration request");

                return StatusCode(500, new
                {
                    success = false,
                    message = "در ثبت درخواست مشکلی رخ داد. لطفاً دوباره تلاش کنید."
                });
            }
        }
    }

    public class CreateCollaborationRequest
    {
        [Required]
        [StringLength(100)]
        public string Specialty { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Experience { get; set; } = string.Empty;

        [Required]
        [StringLength(300)]
        public string Skills { get; set; } = string.Empty;

        [Url]
        [StringLength(500)]
        public string? PortfolioUrl { get; set; }

        [Required]
        [StringLength(1500)]
        public string Description { get; set; } = string.Empty;
    }
}
