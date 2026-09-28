namespace Codino.Api.Models
{
    public class CaptchaRedis
    {
        public string Code { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }
    }
}