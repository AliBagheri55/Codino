namespace Codino.Models
{
    public class User
    {
        // شناسه یکتای کاربر
        public int Id { get; set; }

        // نام و نام خانوادگی
        public string FullName { get; set; } = string.Empty;

        // شماره تلفن کاربر
        public string PhoneNumber { get; set; } = string.Empty;

        // هش رمز عبور
        public string? PasswordHash { get; set; }

        // وضعیت تأیید شماره تلفن
        public bool IsPhoneVerified { get; set; }

        // تاریخ ثبت‌نام
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // آدرس تصویر پروفایل
        public string? ProfileImageUrl { get; set; }

        // مجموع امتیازهای کسب‌شده
        public int TotalPoints { get; set; } = 0;

        // وضعیت فعال بودن حساب
        public bool IsActive { get; set; } = true;

        // اجازه نمایش در تالار افتخار
        public bool ShowInHallOfFame { get; set; } = false;
    }
}