
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Codino.Api.Models
{
    public class TicketMessage
    {
        public int Id { get; set; }

        // شناسه تیکت مربوط به پیام
        [Required]
        public int TicketId { get; set; }

        // رابطه پیام با تیکت
        [ForeignKey(nameof(TicketId))]
        public Ticket? Ticket { get; set; }

        // شناسه ارسال‌کننده پیام
        [Required]
        public int SenderUserId { get; set; }

        // مشخص می‌کند پیام از طرف مدیر است یا کاربر
        public bool IsAdmin { get; set; }

        // متن پیام
        [Required]
        [MaxLength(5000)]
        public string Message { get; set; } = string.Empty;

        // تاریخ ارسال پیام
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
