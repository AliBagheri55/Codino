using System.ComponentModel.DataAnnotations;

namespace Codino.Api.DTOs
{
    public class CreateCommentRequest
    {
        // =========================================================
        // USER ID
        // =========================================================

        [Range(
            1,
            int.MaxValue,
            ErrorMessage = "شناسه کاربر معتبر نیست.")]
        public int UserId { get; set; }


        // =========================================================
        // COMMENT TEXT
        // =========================================================

        [Required(
            ErrorMessage = "متن نظر الزامی است.")]

        [StringLength(
            500,
            MinimumLength = 1,
            ErrorMessage =
                "متن نظر باید بین ۱ تا ۵۰۰ کاراکتر باشد.")]
        public string Text { get; set; } = string.Empty;


        // =========================================================
        // RATING
        // =========================================================

        [Range(
            1,
            5,
            ErrorMessage =
                "امتیاز باید بین ۱ تا ۵ باشد.")]
        public int Rating { get; set; }
    }
}