
using System.ComponentModel.DataAnnotations;

namespace Codino.Api.Models
{
    public class Ticket
    {
        public int Id { get; set; }

        // شماره اختصاصی قابل نمایش به کاربر
        [Required]
        [MaxLength(20)]
        public string TicketNumber { get; set; } = string.Empty;

        // شناسه صاحب تیکت
        [Required]
        public int UserId { get; set; }

        // عنوان تیکت
        [Required]
        [MaxLength(150)]
        public string Subject { get; set; } = string.Empty;

        // دسته‌بندی تیکت
        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = "General";

        // اولویت: Low / Normal / High
        [Required]
        [MaxLength(20)]
        public string Priority { get; set; } = "Normal";

        // وضعیت تیکت
        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "Open";

        // تاریخ ایجاد
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        // آخرین بروزرسانی
        public DateTime? UpdatedAtUtc { get; set; }

        // زمان آخرین پیام
        public DateTime? LastMessageAtUtc { get; set; }

        // فهرست پیام‌های مربوط به این تیکت
        public ICollection<TicketMessage> Messages { get; set; }
            = new List<TicketMessage>();
    }
}
