namespace Codino.Models.Api
{
    /// <summary>
    /// پاسخ API برای ورود کاربر.
    /// </summary>
    public class LoginResponse
    {
        // =========================================================
        // RESULT
        // =========================================================

        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        // =========================================================
        // USER
        // =========================================================

        public int? UserId { get; set; }

        public string? FullName { get; set; }

        // =========================================================
        // JWT
        // =========================================================

        public string? AccessToken { get; set; }

        public string? RefreshToken { get; set; }

        public DateTime? AccessTokenExpiresAt { get; set; }

        // برای سازگاری با کدهای قدیمی
        public string? Token { get; set; }
    }
}