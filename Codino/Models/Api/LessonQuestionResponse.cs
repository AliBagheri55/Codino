
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;

namespace Codino.Models.Api
{
    /// <summary>
    /// مدل سؤال آموزشی برای جلسات دوره متنی کدینو.
    /// پاسخ صحیح و توضیحات پاسخ نباید در JSON عمومی ارسال شوند.
    /// </summary>
    public class LessonQuestionResponse
    {
        // =====================================================
        // BASIC INFORMATION
        // =====================================================

        public int Id { get; set; }

        public int CourseSessionId { get; set; }

        /// <summary>
        /// گزینه‌های قابل نمایش برای سؤال.
        /// این فهرست نباید شامل پاسخ صحیح یا اطلاعات محرمانه باشد.
        /// </summary>
        public List<LessonQuestionOptionResponse> Options { get; set; }
            = new List<LessonQuestionOptionResponse>();

        /// <summary>
        /// زبان برنامه‌نویسی مرتبط با سؤال، در صورت نیاز.
        /// برای مثال: csharp
        /// </summary>
        [StringLength(
            50,
            ErrorMessage = "نام زبان نباید بیشتر از ۵۰ کاراکتر باشد.")]
        public string? CodeLanguage { get; set; }

        /// <summary>
        /// متن سؤال.
        /// در سؤال جای خالی می‌توان از _____ استفاده کرد.
        /// </summary>
        [Required(ErrorMessage = "متن سؤال الزامی است.")]
        [StringLength(
            2000,
            MinimumLength = 1,
            ErrorMessage = "متن سؤال باید حداکثر ۲۰۰۰ کاراکتر باشد.")]
        public string QuestionText { get; set; } = string.Empty;

        /// <summary>
        /// ترتیب نمایش سؤال در جلسه.
        /// </summary>
        [Range(
            1,
            int.MaxValue,
            ErrorMessage = "ترتیب سؤال باید حداقل ۱ باشد.")]
        public int Order { get; set; } = 1;


        // =====================================================
        // QUESTION TYPE
        // =====================================================

        /// <summary>
        /// نوع سؤال:
        /// FillInTheBlank = جای خالی
        /// ShortAnswer = پاسخ کوتاه
        /// </summary>
        [Required(ErrorMessage = "نوع سؤال الزامی است.")]
        [RegularExpression(
            "^(FillInTheBlank|ShortAnswer)$",
            ErrorMessage = "نوع سؤال معتبر نیست.")]
        public string QuestionType { get; set; } = "FillInTheBlank";


        // =====================================================
        // ANSWER INFORMATION
        // =====================================================

        /// <summary>
        /// پاسخ صحیح.
        /// این مقدار نباید در JSON عمومی ارسال شود.
        /// </summary>
        [JsonIgnore]
        public string CorrectAnswer { get; set; } = string.Empty;

        /// <summary>
        /// توضیحات پاسخ صحیح.
        /// فقط پس از ارزیابی سمت سرور و با مجوز مناسب
        /// باید به کاربر ارائه شود.
        /// </summary>
        [JsonIgnore]
        public string Explanation { get; set; } = string.Empty;

        /// <summary>
        /// راهنمای اختیاری سؤال.
        /// </summary>
        [StringLength(
            1000,
            ErrorMessage = "راهنما نباید بیشتر از ۱۰۰۰ کاراکتر باشد.")]
        public string? Hint { get; set; }


        // =====================================================
        // SCORING
        // =====================================================

        /// <summary>
        /// امتیاز سؤال.
        /// </summary>
        [Range(
            1,
            1000,
            ErrorMessage = "امتیاز سؤال باید بین ۱ تا ۱۰۰۰ باشد.")]
        public int Score { get; set; } = 1;


        // =====================================================
        // STATUS
        // =====================================================

        /// <summary>
        /// فعال بودن سؤال.
        /// </summary>
        public bool IsActive { get; set; } = true;


        // =====================================================
        // COMPUTED PROPERTIES
        // =====================================================

        /// <summary>
        /// آیا سؤال از نوع جای خالی است؟
        /// </summary>
        [JsonIgnore]
        public bool IsFillInTheBlank =>
            string.Equals(
                QuestionType?.Trim(),
                "FillInTheBlank",
                StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// آیا سؤال از نوع پاسخ کوتاه است؟
        /// </summary>
        [JsonIgnore]
        public bool IsShortAnswer =>
            string.Equals(
                QuestionType?.Trim(),
                "ShortAnswer",
                StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// آیا سؤال گزینه قابل نمایش دارد؟
        /// </summary>
        [JsonIgnore]
        public bool HasOptions =>
            Options?.Any(option =>
                option != null && option.IsValid) == true;

        /// <summary>
        /// بررسی اولیه اطلاعات سؤال.
        /// این ویژگی جایگزین اعتبارسنجی سمت سرور نیست.
        /// </summary>
        [JsonIgnore]
        public bool HasValidBasicInformation
        {
            get
            {
                bool validQuestionType =
                    IsFillInTheBlank || IsShortAnswer;

                bool validCodeLanguage =
                    CodeLanguage == null ||
                    CodeLanguage.Length <= 50;

                bool validOptions =
                    Options != null &&
                    Options.All(option =>
                        option != null &&
                        option.IsValid);

                bool validHint =
                    Hint == null ||
                    Hint.Length <= 1000;

                return
                    Id >= 0 &&
                    CourseSessionId > 0 &&
                    !string.IsNullOrWhiteSpace(QuestionText) &&
                    QuestionText.Trim().Length <= 2000 &&
                    Order > 0 &&
                    validQuestionType &&
                    validCodeLanguage &&
                    validOptions &&
                    !string.IsNullOrWhiteSpace(CorrectAnswer) &&
                    Score >= 1 &&
                    Score <= 1000 &&
                    validHint;
            }
        }
    }
}