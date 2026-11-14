namespace Codino.Api.Models
{
    public class User
    {
        // =========================================================
        // PRIMARY KEY
        // =========================================================

        public int Id { get; set; }

        // =========================================================
        // USER INFORMATION
        // =========================================================

        /// <summary>
        /// نام و نام خانوادگی کاربر
        /// </summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// شماره موبایل کاربر
        /// </summary>
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>
        /// آدرس عکس پروفایل کاربر
        /// </summary>
        public string? ProfileImageUrl { get; set; }

        // =========================================================
        // AUTHENTICATION
        // =========================================================

        /// <summary>
        /// هش رمز عبور کاربر
        /// </summary>
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// وضعیت تأیید شماره موبایل
        /// </summary>
        public bool IsPhoneVerified { get; set; }

        // =========================================================
        // ACCOUNT INFORMATION
        // =========================================================

        /// <summary>
        /// تاریخ ایجاد حساب
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // =========================================================
        // PASSWORD RECOVERY
        // =========================================================

        /// <summary>
        /// هش اولین کد امنیتی بازیابی رمز عبور
        /// </summary>
        public string SecurityCode1Hash { get; set; } = string.Empty;

        /// <summary>
        /// هش دومین کد امنیتی بازیابی رمز عبور
        /// </summary>
        public string SecurityCode2Hash { get; set; } = string.Empty;

        // =========================================================
        // ACCOUNT STATUS
        // =========================================================

        /// <summary>
        /// حساب فعال یعنی شماره موبایل تأیید شده است.
        /// این ویژگی محاسباتی است و در دیتابیس ذخیره نمی‌شود.
        /// </summary>
        public bool IsActive => IsPhoneVerified;

        // =========================================================
        // CODINO POINTS
        // =========================================================

        /// <summary>
        /// مجموع امتیازهای کاربر در کدینو.
        /// مقدار امتیاز باید توسط سرور مدیریت شود.
        /// </summary>
        public int TotalPoints { get; set; } = 0;

        // =========================================================
        // HALL OF FAME
        // =========================================================

        /// <summary>
        /// اجازه نمایش اطلاعات کاربر در تالار افتخار
        /// </summary>
        public bool ShowInHallOfFame { get; set; } = false;
    }
}