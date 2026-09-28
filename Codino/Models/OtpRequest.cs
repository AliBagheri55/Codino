namespace Codino.Models
{
    public class OtpRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        public string Purpose { get; set; } = string.Empty;
    }
}