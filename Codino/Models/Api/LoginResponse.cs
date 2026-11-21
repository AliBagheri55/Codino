namespace Codino.Models.Api
{
    public class LoginResponse
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public int? UserId { get; set; }

        public string? FullName { get; set; }

        // نقش کاربر: Admin یا User
        public string Role { get; set; } = "User";

        public string? AccessToken { get; set; }

        public string? RefreshToken { get; set; }

        public DateTime? AccessTokenExpiresAt { get; set; }

        public string? Token { get; set; }
    }
}