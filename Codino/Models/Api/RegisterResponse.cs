namespace Codino.Models.Api
{
    /// <summary>
    /// پاسخ API بعد از ثبت‌نام اولیه.
    /// در این مرحله کاربر هنوز تأیید نهایی نشده است.
    /// </summary>
    public class RegisterResponse
    {
        // =========================================================
        // RESULT
        // =========================================================

        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        // =========================================================
        // USER INFORMATION
        // =========================================================

        public int? UserId { get; set; }

        public string? PhoneNumber { get; set; }

        // =========================================================
        // SECURITY CODES
        // =========================================================

        // این دو کد فقط در زمان ثبت‌نام اولیه
        // از API دریافت می‌شوند.
        //
        // خود API فقط Hash آنها را در دیتابیس نگه می‌دارد.

        public string? SecurityCode1 { get; set; }

        public string? SecurityCode2 { get; set; }
    }
}