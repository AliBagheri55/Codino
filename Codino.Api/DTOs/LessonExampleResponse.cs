
using System.ComponentModel.DataAnnotations;

namespace Codino.Api.Dtos
{
    /// <summary>
    /// اطلاعات قابل نمایش یک مثال آموزشی در خروجی API.
    /// </summary>
    public class LessonExampleResponse
    {
        /// <summary>
        /// شناسه یکتای مثال آموزشی.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// عنوان مثال آموزشی.
        /// </summary>
        [Required(ErrorMessage = "عنوان مثال الزامی است.")]
        [StringLength(
            200,
            MinimumLength = 1,
            ErrorMessage = "عنوان مثال باید بین ۱ تا ۲۰۰ کاراکتر باشد.")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// توضیحات مثال آموزشی.
        /// </summary>
        [StringLength(
            2000,
            ErrorMessage = "توضیحات مثال نمی‌تواند بیشتر از ۲۰۰۰ کاراکتر باشد.")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// کد نمونه قابل نمایش به کاربر.
        /// </summary>
        [StringLength(
            20000,
            ErrorMessage = "کد مثال نمی‌تواند بیشتر از ۲۰۰۰۰ کاراکتر باشد.")]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// ترتیب نمایش مثال در جلسه؛ مقدار صفر یا بیشتر.
        /// </summary>
        [Range(
            0,
            int.MaxValue,
            ErrorMessage = "ترتیب نمایش باید صفر یا یک عدد مثبت باشد.")]
        public int Order { get; set; }
    }
}
