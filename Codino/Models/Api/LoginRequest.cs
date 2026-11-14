using System.ComponentModel.DataAnnotations;

namespace Codino.Models.Api
{
    /// <summary>
    /// اطلاعات ورود کاربر.
    /// </summary>
    public class LoginRequest
    {
        [Required(ErrorMessage = "شماره موبایل الزامی است.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "رمز عبور الزامی است.")]
        public string Password { get; set; } = string.Empty;
    }
}