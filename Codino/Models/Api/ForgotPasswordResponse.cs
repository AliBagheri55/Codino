namespace Codino.Models.Api
{
    /// <summary>
    /// پاسخ API برای درخواست بازیابی رمز عبور.
    /// </summary>
    public class ForgotPasswordResponse
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        // توکن موقت تغییر رمز عبور
        public string? ResetToken { get; set; }
    }
}