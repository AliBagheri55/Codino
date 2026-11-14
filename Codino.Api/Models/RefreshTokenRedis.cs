namespace Codino.Api.Models
{
    public class RefreshTokenRedis
    {
        public int UserId { get; set; }

        public string TokenHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime ExpiresAt { get; set; }

        public bool Revoked { get; set; }
    }
}