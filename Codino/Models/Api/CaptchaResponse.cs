namespace Codino.Models.Api
{
    public class CaptchaResponse
    {
        public bool Success { get; set; }

        public string CaptchaId { get; set; } = string.Empty;

        public string Image { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;
    }
}