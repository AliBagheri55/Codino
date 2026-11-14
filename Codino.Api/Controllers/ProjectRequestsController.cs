
using Codino.Api.Data;
using Codino.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace Codino.Api.Controllers
{
    [ApiController]
    [Route("api/project-requests")]
    public class ProjectRequestsController : ControllerBase
    {
        private static readonly HashSet<string> AllowedProjectTypes =
            new(StringComparer.Ordinal)
            {
                "Web Application",
                "Desktop Application",
                "Mobile Application",
                "AI and Machine Learning",
                "Game Development",
                "Automation",
                "Other"
            };

        private static readonly HashSet<string> AllowedBudgets =
            new(StringComparer.Ordinal)
            {
                "کمتر از 5 میلیون تومان",
                "5 تا 10 میلیون تومان",
                "10 تا 20 میلیون تومان",
                "20 تا 50 میلیون تومان",
                "بیشتر از 50 میلیون تومان"
            };

        private readonly CodinoDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ProjectRequestsController> _logger;

        public ProjectRequestsController(
            CodinoDbContext context,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<ProjectRequestsController> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create(
            [FromBody] CreateProjectRequestDto? request,
            CancellationToken cancellationToken)
        {
            if (request is null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "اطلاعات درخواست ارسال نشده است."
                });
            }

            // اعتبارسنجی نوع پروژه
            if (!string.IsNullOrWhiteSpace(request.ProjectType) &&
                !AllowedProjectTypes.Contains(request.ProjectType.Trim()))
            {
                ModelState.AddModelError(
                    nameof(request.ProjectType),
                    "نوع پروژه انتخاب‌شده معتبر نیست.");
            }

            // اعتبارسنجی بودجه اختیاری
            if (!string.IsNullOrWhiteSpace(request.Budget) &&
                !AllowedBudgets.Contains(request.Budget.Trim()))
            {
                ModelState.AddModelError(
                    nameof(request.Budget),
                    "مقدار بودجه انتخاب‌شده معتبر نیست.");
            }

            // جلوگیری از تاریخ تحویل گذشته
            if (request.DesiredDeadline.HasValue &&
                request.DesiredDeadline.Value.Date < DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(request.DesiredDeadline),
                    "تاریخ تحویل نمی‌تواند در گذشته باشد.");
            }

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            // پاک‌سازی ورودی‌ها
            var customerName = request.CustomerName.Trim();
            var email = request.Email.Trim();
            var phoneNumber = request.PhoneNumber.Trim();
            var projectName = request.ProjectName.Trim();
            var projectType = request.ProjectType.Trim();
            var description = request.Description.Trim();

            if (string.IsNullOrWhiteSpace(customerName) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(phoneNumber) ||
                string.IsNullOrWhiteSpace(projectName) ||
                string.IsNullOrWhiteSpace(projectType) ||
                string.IsNullOrWhiteSpace(description))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "لطفاً تمام فیلدهای اجباری را تکمیل کنید."
                });
            }

            // بررسی شماره موبایل ایران
            if (!Regex.IsMatch(phoneNumber, @"\A09[0-9]{9}\z"))
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "شماره موبایل باید با 09 شروع شود و دقیقاً 11 رقم باشد."
                });
            }

            var projectRequest = new ProjectRequest
            {
                CustomerName = customerName,
                Email = email,
                PhoneNumber = phoneNumber,
                ProjectName = projectName,
                ProjectType = projectType,
                Description = description,

                Budget = string.IsNullOrWhiteSpace(request.Budget)
                    ? null
                    : request.Budget.Trim(),

                DesiredDeadline = request.DesiredDeadline,
                Status = "Pending",
                CreatedAtUtc = DateTime.UtcNow
            };

            // مرحله اول: ذخیره درخواست در دیتابیس
            try
            {
                _context.ProjectRequests.Add(projectRequest);

                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Project request saving was canceled.");

                // درخواست لغوشده را به‌عنوان ثبت موفق گزارش نمی‌کنیم.
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Database error while saving a project request.");

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        success = false,
                        message =
                            "ذخیره درخواست در دیتابیس ناموفق بود. لطفاً بعداً دوباره تلاش کنید."
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while saving a project request.");

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        success = false,
                        message =
                            "خطای غیرمنتظره‌ای هنگام ثبت درخواست رخ داد."
                    });
            }

            // مرحله دوم: ارسال اعلان تلگرام
            // شکست اعلان نباید درخواست ذخیره‌شده را ناموفق جلوه دهد.
            try
            {
                await SendTelegramNotificationAsync(projectRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Telegram notification failed for project {ProjectId}.",
                    projectRequest.Id);
            }

            // مرحله سوم: پاسخ موفق به سایت
            return StatusCode(StatusCodes.Status201Created, new
            {
                success = true,
                message =
                    "درخواست شما با موفقیت ثبت شد. کارشناسان کدینو پس از بررسی با شما تماس خواهند گرفت.",
                id = projectRequest.Id,
                status = projectRequest.Status
            });
        }

        private async Task SendTelegramNotificationAsync(
            ProjectRequest request)
        {
            var token = _configuration["Telegram:BotToken"];

            var chatId =
                _configuration["Telegram:AdminChatId"]
                ?? _configuration["Telegram:AdminUserId"];

            if (string.IsNullOrWhiteSpace(token) ||
                string.IsNullOrWhiteSpace(chatId))
            {
                _logger.LogWarning(
                    "Telegram notification skipped: bot token or chat ID is missing.");

                return;
            }

            var deadline = request.DesiredDeadline.HasValue
                ? request.DesiredDeadline.Value.ToString("yyyy/MM/dd")
                : "مشخص نشده";

            var description = request.Description.Length > 2000
                ? request.Description[..2000] + "..."
                : request.Description;

            var message =
                $"🆕 درخواست پروژه جدید کدینو\n\n" +
                $"🆔 شماره درخواست: {request.Id}\n" +
                $"👤 نام: {request.CustomerName}\n" +
                $"📧 ایمیل: {request.Email}\n" +
                $"📱 تلفن: {request.PhoneNumber}\n" +
                $"💻 پروژه: {request.ProjectName}\n" +
                $"🧩 نوع: {request.ProjectType}\n" +
                $"💰 بودجه: {request.Budget ?? "مشخص نشده"}\n" +
                $"📅 مهلت: {deadline}\n\n" +
                $"📝 توضیحات:\n{description}\n\n" +
                $"وضعیت: در انتظار بررسی";

            var payload = new
            {
                chat_id = chatId,
                text = message,
                reply_markup = new
                {
                    inline_keyboard = new object[][]
                    {
                        new object[]
                        {
                            new
                            {
                                text = " تأیید درخواست",
                                callback_data =
                                    $"project:approve:{request.Id}"
                            },
                            new
                            {
                                text = " رد درخواست",
                                callback_data =
                                    $"project:reject:{request.Id}"
                            }
                        }
                    }
                }
            };

            try
            {
                var client = _httpClientFactory.CreateClient();

                // محدودیت زمانی مستقل از درخواست کاربر
                using var timeoutCts =
                    new CancellationTokenSource(
                        TimeSpan.FromSeconds(10));

                using var response = await client.PostAsJsonAsync(
                    $"https://api.telegram.org/bot{token}/sendMessage",
                    payload,
                    timeoutCts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    var responseBody =
                        await response.Content.ReadAsStringAsync();

                    _logger.LogWarning(
                        "Telegram returned HTTP {StatusCode}. Response: {Response}",
                        (int)response.StatusCode,
                        responseBody);

                    return;
                }

                _logger.LogInformation(
                    "Telegram notification sent for project {ProjectId}.",
                    request.Id);
            }
            catch (OperationCanceledException ex)
            {
                // شامل timeout درخواست تلگرام می‌شود.
                _logger.LogWarning(
                    ex,
                    "Telegram notification timed out or was canceled for project {ProjectId}.",
                    request.Id);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "Network error sending Telegram notification for project {ProjectId}.",
                    request.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected Telegram notification error for project {ProjectId}.",
                    request.Id);
            }
        }
    }

    public class CreateProjectRequestDto
    {
        [Required(ErrorMessage = "نام شما الزامی است.")]
        [StringLength(100, MinimumLength = 2)]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "ایمیل الزامی است.")]
        [EmailAddress(ErrorMessage = "ایمیل معتبر نیست.")]
        [StringLength(254)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "شماره تماس الزامی است.")]
        [RegularExpression(
            @"\A09[0-9]{9}\z",
            ErrorMessage =
                "شماره موبایل باید با 09 شروع شود و دقیقاً 11 رقم باشد.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "عنوان پروژه الزامی است.")]
        [StringLength(150, MinimumLength = 3)]
        public string ProjectName { get; set; } = string.Empty;

        [Required(ErrorMessage = "نوع پروژه را انتخاب کنید.")]
        [StringLength(80)]
        public string ProjectType { get; set; } = string.Empty;

        [Required(ErrorMessage = "توضیحات پروژه الزامی است.")]
        [StringLength(
            5000,
            MinimumLength = 20,
            ErrorMessage =
                "توضیحات باید بین ۲۰ تا ۵۰۰۰ کاراکتر باشد.")]
        public string Description { get; set; } = string.Empty;

        [StringLength(80)]
        public string? Budget { get; set; }

        public DateTime? DesiredDeadline { get; set; }
    }
}
