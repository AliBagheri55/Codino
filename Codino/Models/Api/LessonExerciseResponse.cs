using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Codino.Models.Api
{
    /// <summary>
    /// مدل پاسخ تمرین برنامه‌نویسی هر جلسه در کدینو.
    /// برای انتقال اطلاعات عمومی تمرین به کلاینت استفاده می‌شود.
    /// </summary>
    public class LessonExerciseResponse
    {
        // =====================================================
        // BASIC INFORMATION
        // =====================================================

        public int Id { get; set; }

        public int CourseSessionId { get; set; }

        [StringLength(
            200,
            ErrorMessage = "عنوان تمرین نباید بیشتر از ۲۰۰ کاراکتر باشد.")]
        public string Title { get; set; } = string.Empty;

        [StringLength(
            5000,
            ErrorMessage = "توضیحات تمرین نباید بیشتر از ۵۰۰۰ کاراکتر باشد.")]
        public string Description { get; set; } = string.Empty;

        [Range(
            1,
            int.MaxValue,
            ErrorMessage = "ترتیب تمرین باید حداقل ۱ باشد.")]
        public int Order { get; set; } = 1;


        // =====================================================
        // INPUT AND OUTPUT
        // =====================================================

        [StringLength(5000)]
        public string SampleInput { get; set; } = string.Empty;

        [StringLength(5000)]
        public string SampleOutput { get; set; } = string.Empty;

        [StringLength(3000)]
        public string Constraints { get; set; } = string.Empty;


        // =====================================================
        // HINT AND SOLUTION
        // =====================================================

        /// <summary>
        /// راهنمای تمرین که در صورت نیاز به کاربر نمایش داده می‌شود.
        /// </summary>
        [StringLength(3000)]
        public string? Hint { get; set; }

        /// <summary>
        /// راه‌حل مرجع تمرین؛ نباید در JSON عمومی ارسال شود.
        /// </summary>
        [JsonIgnore]
        public string? Solution { get; set; }

        /// <summary>
        /// توضیح راه‌حل مرجع؛ نباید در JSON عمومی ارسال شود.
        /// </summary>
        [JsonIgnore]
        public string? SolutionExplanation { get; set; }


        // =====================================================
        // CODE SUBMISSION
        // =====================================================

        [StringLength(50)]
        public string ProgrammingLanguage { get; set; } = "csharp";

        [StringLength(
            10000,
            ErrorMessage = "کد اولیه نباید بیشتر از ۱۰۰۰۰ کاراکتر باشد.")]
        public string? StarterCode { get; set; }

        [Range(
            1,
            10000,
            ErrorMessage = "حداکثر طول کد باید بین ۱ تا ۱۰۰۰۰ باشد.")]
        public int MaxCodeLength { get; set; } = 10000;

        public bool RequiresCodeSubmission { get; set; } = true;


        // =====================================================
        // SCORING
        // =====================================================

        [Range(
            1,
            100000,
            ErrorMessage = "امتیاز تمرین باید حداقل ۱ باشد.")]
        public int Score { get; set; } = 10;

        [Range(
            0,
            100000,
            ErrorMessage = "امتیاز قبولی معتبر نیست.")]
        public int PassingScore { get; set; } = 10;

        /// <summary>
        /// صفر یعنی محدودیت تعداد تلاش‌ها تعریف نشده است.
        /// سرور باید محدودیت واقعی تلاش‌ها را اعمال کند.
        /// </summary>
        [Range(0, 1000)]
        public int MaxAttempts { get; set; }


        // =====================================================
        // STATUS
        // =====================================================

        public bool IsActive { get; set; } = true;

        public bool IsFree { get; set; }


        // =====================================================
        // VALIDATION
        // =====================================================

        /// <summary>
        /// بررسی اولیه سازگاری اطلاعات تمرین.
        /// برای اعتبارسنجی امنیتی، بررسی نهایی سمت سرور الزامی است.
        /// </summary>
        [JsonIgnore]
        public bool HasValidBasicInformation
        {
            get
            {
                string language = ProgrammingLanguage?.Trim() ?? string.Empty;

                return
                    Id >= 0 &&
                    CourseSessionId > 0 &&

                    !string.IsNullOrWhiteSpace(Title) &&
                    Title.Length <= 200 &&

                    !string.IsNullOrWhiteSpace(Description) &&
                    Description.Length <= 5000 &&

                    Order > 0 &&

                    SampleInput != null &&
                    SampleInput.Length <= 5000 &&

                    SampleOutput != null &&
                    SampleOutput.Length <= 5000 &&

                    Constraints != null &&
                    Constraints.Length <= 3000 &&

                    (Hint == null || Hint.Length <= 3000) &&

                    Score >= 1 &&
                    Score <= 100000 &&

                    PassingScore >= 0 &&
                    PassingScore <= Score &&

                    MaxCodeLength >= 1 &&
                    MaxCodeLength <= 10000 &&

                    MaxAttempts >= 0 &&
                    MaxAttempts <= 1000 &&

                    (StarterCode == null ||
                     StarterCode.Length <= 10000) &&

                    language.Length <= 50 &&
                    string.Equals(
                        language,
                        "csharp",
                        StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}