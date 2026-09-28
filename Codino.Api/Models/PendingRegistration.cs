namespace Codino.Api.Models
{
    public class PendingRegistration
    {
        public string FullName { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string Otp { get; set; } = string.Empty;

        public DateTime OtpExpiresAt { get; set; }
    }
}