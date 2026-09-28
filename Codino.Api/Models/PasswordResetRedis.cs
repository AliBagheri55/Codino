namespace Codino.Api.Models
{
    public class PasswordResetRedis
    {
        public string PhoneNumber { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }
    }
}