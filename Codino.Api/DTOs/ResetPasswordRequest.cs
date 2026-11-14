using System.ComponentModel.DataAnnotations;

namespace Codino.Api.DTOs
{
    public class ResetPasswordRequest
    {
        [Required(ErrorMessage = "شماره موبایل الزامی است.")]
        [RegularExpression(
            @"^09\d{9}$",
            ErrorMessage = "شماره موبایل معتبر نیست.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "ResetToken الزامی است.")]
        public string ResetToken { get; set; } = string.Empty;

        [Required(ErrorMessage = "رمز عبور جدید الزامی است.")]
        [StringLength(
            128,
            MinimumLength = 8,
            ErrorMessage = "رمز عبور باید بین 8 تا 128 کاراکتر باشد.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "تکرار رمز عبور الزامی است.")]
        public string RepeatPassword { get; set; } = string.Empty;
    }
}