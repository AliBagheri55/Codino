
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
        // COURSES
        // =====================================================

        /// <summary>
        /// فهرست دوره‌های آموزشی قابل نمایش در صفحه اصلی.
        /// </summary>
       
        /// <summary>
        /// آیا دوره‌ای برای نمایش وجود دارد؟
        /// </summary>
      
        /// <summary>
        /// تعداد دوره‌های غیر null.
        /// </summary>
     

        // =====================================================
        // SUMMARY
        // =====================================================

        /// <summary>
        /// آیا صفحه اصلی هیچ نظری یا دوره‌ای برای نمایش ندارد؟
        /// </summary>
     
    }
}
