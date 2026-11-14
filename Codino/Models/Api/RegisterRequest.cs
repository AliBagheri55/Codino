using System.ComponentModel.DataAnnotations;

namespace Codino.Models.Api
{
    /// <summary>
    /// اطلاعات ثبت‌نام کاربر.
    /// </summary>
    public class RegisterRequest
    {
        [Required(ErrorMessage = "نام و نام خانوادگی الزامی است.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "شماره موبایل الزامی است.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "رمز عبور الزامی است.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "تکرار رمز عبور الزامی است.")]
        public string RepeatPassword { get; set; } = string.Empty;
    }
}