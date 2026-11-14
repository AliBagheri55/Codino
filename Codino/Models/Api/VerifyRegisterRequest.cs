using System.ComponentModel.DataAnnotations;

namespace Codino.Models.Api
{
    /// <summary>
    /// اطلاعات تأیید ثبت‌نام.
    /// </summary>
    public class VerifyRegisterRequest
    {
        [Required(ErrorMessage = "شماره موبایل الزامی است.")]
        public string PhoneNumber { get; set; } = string.Empty;

        // کد امنیتی 4 رقمی
        [Required(ErrorMessage = "کد امنیتی الزامی است.")]
        public string Code { get; set; } = string.Empty;
    }
}