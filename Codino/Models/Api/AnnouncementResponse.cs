
using System;

namespace Codino.Models.Api
{
    /// <summary>
    /// اطلاعات اطلاعیه دریافتی از API.
    /// </summary>
    public class AnnouncementResponse
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public string AuthorLabel { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }
    }
}
