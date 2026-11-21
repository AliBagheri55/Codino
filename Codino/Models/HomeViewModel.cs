
using System.Collections.Generic;
using System.Linq;
using Codino.Models.Api;

namespace Codino.Models
{
    /// <summary>
    /// مدل اطلاعات موردنیاز صفحه اصلی کدینو.
    /// </summary>
    public class HomeViewModel
    {
        // =====================================================
        // COMMENTS
        // =====================================================

        /// <summary>
        /// نظرات قابل نمایش در صفحه اصلی.
        /// </summary>
        public List<CommentResponse> Comments { get; set; } =
            new List<CommentResponse>();

        /// <summary>
        /// آیا نظری برای نمایش وجود دارد؟
        /// </summary>
        public bool HasComments =>
            Comments != null &&
            Comments.Any(comment => comment != null);

        /// <summary>
        /// تعداد نظرات غیر null.
        /// </summary>
        public int CommentCount =>
            Comments?.Count(comment => comment != null) ?? 0;


        // =====================================================
        // ANNOUNCEMENTS
        // =====================================================

        /// <summary>
        /// اطلاعیه‌های منتشرشده قابل نمایش در صفحه اصلی.
        /// </summary>
        public List<AnnouncementResponse> Announcements { get; set; } =
            new List<AnnouncementResponse>();

        /// <summary>
        /// آیا اطلاعیه‌ای برای نمایش وجود دارد؟
        /// </summary>
        public bool HasAnnouncements =>
            Announcements != null &&
            Announcements.Any(announcement => announcement != null);


        // =====================================================
        // COURSES
        // =====================================================

        // بخش دوره‌ها در آینده تکمیل خواهد شد.


        // =====================================================
        // SUMMARY
        // =====================================================

        // بخش خلاصه اطلاعات صفحه اصلی در آینده تکمیل خواهد شد.
    }
}
