namespace Codino.Api.Models
{
    public class User
    {
        public int Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string? ProfileImageUrl { get; set; }

        public string PasswordHash { get; set; } = string.Empty;

        public bool IsPhoneVerified { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string SecurityCode1Hash { get; set; } = string.Empty;

        public string SecurityCode2Hash { get; set; } = string.Empty;

        // نقش کاربر؛ ثبت‌نام‌های عادی به‌صورت پیش‌فرض User هستند.
        public string Role { get; set; } = "User";

        public bool IsActive => IsPhoneVerified;

        public int TotalPoints { get; set; } = 0;

        public bool ShowInHallOfFame { get; set; } = false;
    }
}