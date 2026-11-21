
namespace Codino.Api.Models.Api
{
    /// <summary>
    /// پاسخ API برای ورود کاربر
    /// </summary>
    public class LoginResponse
    {
        // نتیجه ورود
        public bool Success { get; set; }

        // پیام نتیجه
        public string Message { get; set; } = string.Empty;

        // اطلاعات کاربر
        public int? UserId { get; set; }

        public string? FullName { get; set; }

        // نقش کاربر
        public string Role { get; set; } = "User";

        // اطلاعات توکن
        public string? AccessToken { get; set; }

        public string? RefreshToken { get; set; }

        public DateTime? AccessTokenExpiresAt { get; set; }
    }
}
