using System.ComponentModel.DataAnnotations;

namespace Codino.Api.DTOs
{
    public class RegisterRequest
    {
        [Required(ErrorMessage = "نام و نام خانوادگی الزامی است.")]
        [StringLength(
            100,
            MinimumLength = 2,
            ErrorMessage = "نام و نام خانوادگی باید بین 2 تا 100 کاراکتر باشد.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "شماره موبایل الزامی است.")]
        [RegularExpression(
            @"^09\d{9}$",
            ErrorMessage = "شماره موبایل معتبر نیست.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "رمز عبور الزامی است.")]
        [StringLength(
            128,
            MinimumLength = 8,
            ErrorMessage = "رمز عبور باید بین 8 تا 128 کاراکتر باشد.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "تکرار رمز عبور الزامی است.")]
        public string RepeatPassword { get; set; } = string.Empty;
    }
}