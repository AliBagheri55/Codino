using System.ComponentModel.DataAnnotations;

namespace Codino.Api.DTOs
{
    public class VerifyRegisterRequest
    {
        [Required(ErrorMessage = "شماره موبایل الزامی است.")]
        [RegularExpression(
            @"^09\d{9}$",
            ErrorMessage = "شماره موبایل معتبر نیست.")]
        public string PhoneNumber { get; set; } = string.Empty;

        // کد امنیتی 4 رقمی
        [Required(ErrorMessage = "کد امنیتی الزامی است.")]
        [RegularExpression(
            @"^\d{4}$",
            ErrorMessage = "کد امنیتی باید 4 رقمی باشد.")]
        public string Code { get; set; } = string.Empty;
    }
}