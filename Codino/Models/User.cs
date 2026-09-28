namespace Codino.Models
{
    public class User
    {
        public int Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string? PasswordHash { get; set; }

        public bool IsPhoneVerified { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}