
using System.ComponentModel.DataAnnotations;

namespace Codino.Api.Models
{
    public class CollaborationRequest
    {
        public int Id { get; set; }

        // شناسه کاربری که درخواست را ثبت کرده است
        public int UserId { get; set; }

        // تخصص برنامه‌نویس
        [Required]
        [MaxLength(100)]
        public string Specialty { get; set; } = string.Empty;

        // میزان تجربه کاری
        [Required]
        [MaxLength(50)]
        public string Experience { get; set; } = string.Empty;

        // زبان‌ها و مهارت‌های برنامه‌نویسی
        [Required]
        [MaxLength(300)]
        public string Skills { get; set; } = string.Empty;

        // لینک نمونه‌کار
        [MaxLength(500)]
        public string? PortfolioUrl { get; set; }

        // توضیحات برنامه‌نویس
        [Required]
        [MaxLength(1500)]
        public string Description { get; set; } = string.Empty;

        // وضعیت درخواست
        [MaxLength(30)]
        public string Status { get; set; } = "Pending";

        // تاریخ ثبت درخواست
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
