using System.ComponentModel.DataAnnotations;

namespace Codino.Models.Api
{
    /// <summary>
    /// اطلاعات مورد نیاز برای تعیین رمز عبور جدید.
    /// </summary>
    public class ResetPasswordRequest
    {
        [Required(ErrorMessage = "شماره موبایل الزامی است.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Reset Token الزامی است.")]
        public string ResetToken { get; set; } = string.Empty;

        [Required(ErrorMessage = "رمز عبور جدید الزامی است.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "تکرار رمز عبور الزامی است.")]
        public string RepeatPassword { get; set; } = string.Empty;
    }
}