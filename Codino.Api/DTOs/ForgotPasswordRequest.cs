using System.ComponentModel.DataAnnotations;

namespace Codino.Api.DTOs
{
    public class ForgotPasswordRequest
    {
        [Required(ErrorMessage = "شماره موبایل الزامی است.")]
        [RegularExpression(
            @"^09\d{9}$",
            ErrorMessage = "شماره موبایل معتبر نیست.")]
        public string PhoneNumber { get; set; } = string.Empty;

        // یکی از دو کد امنیتی 4 رقمی کاربر
        [Required(ErrorMessage = "کد امنیتی الزامی است.")]
        [RegularExpression(
            @"^\d{4}$",
            ErrorMessage = "کد امنیتی باید 4 رقمی باشد.")]
        public string SecurityCode { get; set; } = string.Empty;

        // شناسه CAPTCHA
        [Required(ErrorMessage = "CaptchaId الزامی است.")]
        public string CaptchaId { get; set; } = string.Empty;

        // مقدار CAPTCHA وارد شده توسط کاربر
        [Required(ErrorMessage = "کد CAPTCHA الزامی است.")]
        [StringLength(
            20,
            MinimumLength = 4,
            ErrorMessage = "کد CAPTCHA نامعتبر است.")]
        public string Captcha { get; set; } = string.Empty;
    }
}