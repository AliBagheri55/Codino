namespace Codino.Api.DTOs
{
    public class ForgotPasswordResponse
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public string? ResetToken { get; set; }
    }
}