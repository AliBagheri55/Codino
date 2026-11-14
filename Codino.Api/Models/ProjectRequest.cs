
using System;
using System.ComponentModel.DataAnnotations;

namespace Codino.Api.Models
{
    public class ProjectRequest
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "نام شما الزامی است.")]
        [StringLength(
            100,
            MinimumLength = 2,
            ErrorMessage = "نام باید بین ۲ تا ۱۰۰ کاراکتر باشد.")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "ایمیل الزامی است.")]
        [EmailAddress(ErrorMessage = "ایمیل معتبر نیست.")]
        [StringLength(
            254,
            ErrorMessage = "ایمیل نمی‌تواند بیشتر از ۲۵۴ کاراکتر باشد.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "شماره تماس الزامی است.")]
        [RegularExpression(
            @"\A09[0-9]{9}\z",
            ErrorMessage = "شماره موبایل باید با 09 شروع شود و دقیقاً ۱۱ رقم باشد.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "عنوان پروژه الزامی است.")]
        [StringLength(
            150,
            MinimumLength = 3,
            ErrorMessage = "عنوان پروژه باید بین ۳ تا ۱۵۰ کاراکتر باشد.")]
        public string ProjectName { get; set; } = string.Empty;

        [Required(ErrorMessage = "نوع پروژه را انتخاب کنید.")]
        [StringLength(
            80,
            ErrorMessage = "نوع پروژه نمی‌تواند بیشتر از ۸۰ کاراکتر باشد.")]
        public string ProjectType { get; set; } = string.Empty;

        [Required(ErrorMessage = "توضیحات پروژه الزامی است.")]
        [StringLength(
            5000,
            MinimumLength = 20,
            ErrorMessage = "توضیحات پروژه باید بین ۲۰ تا ۵۰۰۰ کاراکتر باشد.")]
        public string Description { get; set; } = string.Empty;

        [StringLength(
            80,
            ErrorMessage = "بودجه نمی‌تواند بیشتر از ۸۰ کاراکتر باشد.")]
        public string? Budget { get; set; }

        public DateTime? DesiredDeadline { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
