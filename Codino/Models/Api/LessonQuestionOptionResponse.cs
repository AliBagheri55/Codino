
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Codino.Models.Api
{
    /// <summary>
    /// مدل پاسخ عمومی یک گزینه از سؤال آزمون.
    /// این مدل فقط اطلاعات قابل نمایش به کاربر را منتقل می‌کند.
    /// </summary>
    public class LessonQuestionOptionResponse
    {
        // =====================================================
        // OPTION INFORMATION
        // =====================================================

        /// <summary>
        /// شناسه گزینه.
        /// </summary>
        [Range(
            0,
            int.MaxValue,
            ErrorMessage = "شناسه گزینه معتبر نیست.")]
        public int Id { get; set; }

        /// <summary>
        /// متن گزینه که به کاربر نمایش داده می‌شود.
        /// </summary>
        [Required(ErrorMessage = "متن گزینه الزامی است.")]
        [StringLength(
            1000,
            MinimumLength = 1,
            ErrorMessage = "متن گزینه باید حداکثر ۱۰۰۰ کاراکتر باشد.")]
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// ترتیب نمایش گزینه در سؤال.
        /// ترتیب باید عددی مثبت باشد.
        /// </summary>
        [Range(
            1,
            int.MaxValue,
            ErrorMessage = "ترتیب گزینه باید حداقل ۱ باشد.")]
        public int Order { get; set; } = 1;


        // =====================================================
        // OPTION STATUS
        // =====================================================

        /// <summary>
        /// آیا متن گزینه قابل نمایش است؟
        /// </summary>
        [JsonIgnore]
        public bool HasText =>
            !string.IsNullOrWhiteSpace(Text);

        /// <summary>
        /// بررسی اولیه معتبر بودن اطلاعات گزینه.
        /// </summary>
        [JsonIgnore]
        public bool IsValid =>
            Id >= 0 &&
            HasText &&
            Text.Trim().Length <= 1000 &&
            Order > 0;
    }
}
