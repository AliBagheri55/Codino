namespace Codino.Api.DTOs
{
    public class ResetPasswordRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;

        public string ResetToken { get; set; } = string.Empty;

        public string NewPassword { get; set; } = string.Empty;

        public string RepeatPassword { get; set; } = string.Empty;
    }
}