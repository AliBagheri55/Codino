namespace Codino.Api.DTOs
{
    public class ForgotPasswordResponse
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        // در صورت موفقیت، توکن موقت تغییر رمز عبور
        public string? ResetToken { get; set; }
    }
}