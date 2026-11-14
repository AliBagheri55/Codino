using System.ComponentModel.DataAnnotations;

namespace Codino.Models.Api
{
    /// <summary>
    /// اطلاعات مورد نیاز برای شروع فرآیند بازیابی رمز عبور.
    /// </summary>
    public class ForgotPasswordRequest
    {
        // =========================================================
        // PHONE NUMBER
        // =========================================================

        [Required(ErrorMessage = "شماره موبایل الزامی است.")]
        public string PhoneNumber { get; set; } = string.Empty;

        // =========================================================
        // SECURITY CODE
        // =========================================================

        // یکی از دو کد امنیتی 4 رقمی
        // که هنگام ثبت‌نام به کاربر داده شده است.
        [Required(ErrorMessage = "کد امنیتی الزامی است.")]
        public string SecurityCode { get; set; } = string.Empty;

        // =========================================================
        // CAPTCHA ID
        // =========================================================

        [Required(ErrorMessage = "شناسه کپچا الزامی است.")]
        public string CaptchaId { get; set; } = string.Empty;

        // =========================================================
        // CAPTCHA
        // =========================================================

        [Required(ErrorMessage = "کد کپچا الزامی است.")]
        public string Captcha { get; set; } = string.Empty;
    }
}