namespace Codino.Models.Api
{
    /// <summary>
    /// پاسخ عمومی API.
    /// </summary>
    public class ApiResponse
    {
        // =========================================================
        // RESULT
        // =========================================================

        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        // =========================================================
        // USER
        // =========================================================

        public int? UserId { get; set; }

        // =========================================================
        // REGISTRATION SECURITY CODES
        // =========================================================

        // این مقادیر فقط در پاسخ ثبت‌نام استفاده می‌شوند.
        // Hash آنها هرگز به MVC ارسال نمی‌شود.

        public string? SecurityCode1 { get; set; }

        public string? SecurityCode2 { get; set; }
    }
}