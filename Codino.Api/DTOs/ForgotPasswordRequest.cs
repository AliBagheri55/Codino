namespace Codino.Api.DTOs
{
    public class ForgotPasswordRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;

        public string CaptchaId { get; set; } = string.Empty;

        public string Captcha { get; set; } = string.Empty;
    }
}