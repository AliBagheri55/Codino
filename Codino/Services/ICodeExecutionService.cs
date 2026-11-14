
namespace Codino.Services
{
    public interface ICodeExecutionService
    {
        // =========================================================
        // اجرای کد C#
        // =========================================================

        /// <summary>
        /// کد C# ارسال‌شده توسط کاربر را در محیط Sandbox اجرا می‌کند.
        /// در این سرویس فقط زبان C# با مقدار "csharp" مجاز است.
        /// </summary>
        /// <param name="request">
        /// اطلاعات درخواست شامل زبان و کد C#.
        /// </param>
        /// <param name="cancellationToken">
        /// توکن لغو عملیات.
        /// </param>
        /// <returns>
        /// نتیجه اجرای کد شامل وضعیت، خروجی، خطا و زمان اجرا.
        /// </returns>
    }
}