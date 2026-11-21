
using System.ComponentModel.DataAnnotations;

namespace Codino.Api.Models
{
    public class Announcement
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(10000)]
        public string Content { get; set; } = string.Empty;

        [MaxLength(100)]
        public string AuthorLabel { get; set; } = "واحد پشتیبانی | Codino";

        public bool IsPublished { get; set; } = true;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAtUtc { get; set; }
    }
}
