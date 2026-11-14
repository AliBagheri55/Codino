
using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace Codino.Controllers
{
    public class ProjectRequestsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ProjectRequestsController> _logger;

        public ProjectRequestsController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<ProjectRequestsController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateProjectRequestViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            CreateProjectRequestViewModel model,
            CancellationToken cancellationToken)
        {
            // پاک‌سازی ورودی‌ها
            model.CustomerName = model.CustomerName?.Trim() ?? string.Empty;
            model.Email = model.Email?.Trim() ?? string.Empty;
            model.PhoneNumber = model.PhoneNumber?.Trim() ?? string.Empty;
            model.ProjectName = model.ProjectName?.Trim() ?? string.Empty;
            model.ProjectType = model.ProjectType?.Trim() ?? string.Empty;
            model.Description = model.Description?.Trim() ?? string.Empty;

            model.Budget = string.IsNullOrWhiteSpace(model.Budget)
                ? null
                : model.Budget.Trim();

            // اعتبارسنجی مجدد اطلاعات پس از پاک‌سازی
            ModelState.Clear();
            TryValidateModel(model);

            // جلوگیری از تاریخ تحویل گذشته
            if (model.DesiredDeadline.HasValue &&
                model.DesiredDeadline.Value.Date < DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(model.DesiredDeadline),
                    "تاریخ تحویل نمی‌تواند در گذشته باشد.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var apiBaseUrl = _configuration["ApiSettings:BaseUrl"];

            if (string.IsNullOrWhiteSpace(apiBaseUrl) ||
                !Uri.TryCreate(
                    apiBaseUrl,
                    UriKind.Absolute,
                    out var baseUri) ||
                (baseUri.Scheme != Uri.UriSchemeHttp &&
                 baseUri.Scheme != Uri.UriSchemeHttps))
            {
                _logger.LogError(
                    "ApiSettings:BaseUrl is missing or invalid.");

                ModelState.AddModelError(
                    string.Empty,
                    "در حال حاضر امکان ارتباط با سرور وجود ندارد.");

                return View(model);
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(15);

                var apiUrl =
                    $"{baseUri.ToString().TrimEnd('/')}/api/project-requests";

                var requestData = new
                {
                    customerName = model.CustomerName,
                    email = model.Email,
                    phoneNumber = model.PhoneNumber,
                    projectName = model.ProjectName,
                    projectType = model.ProjectType,
                    description = model.Description,
                    budget = model.Budget,
                    desiredDeadline = model.DesiredDeadline
                };

                using var response = await client.PostAsJsonAsync(
                    apiUrl,
                    requestData,
                    cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "درخواست شما با موفقیت ثبت شد. " +
                        "به‌زودی جهت تأیید یا رد پروژه شما " +
                        "با شما تماس خواهیم گرفت.";

                    return RedirectToAction(nameof(Create));
                }

                _logger.LogWarning(
                    "Project request API returned status code {StatusCode}.",
                    (int)response.StatusCode);

                ModelState.AddModelError(
                    string.Empty,
                    "ثبت درخواست ناموفق بود. لطفاً اطلاعات را بررسی کنید و دوباره تلاش کنید.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "Could not connect to the project requests API.");

                ModelState.AddModelError(
                    string.Empty,
                    "ارتباط با سرور برقرار نشد. لطفاً کمی بعد دوباره تلاش کنید.");
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(
                    "Project request API timed out.");

                ModelState.AddModelError(
                    string.Empty,
                    "پاسخ سرور بیش از حد طول کشید. لطفاً دوباره تلاش کنید.");
            }

            return View(model);
        }
    }

    public class CreateProjectRequestViewModel
    {
        [Required(ErrorMessage = "نام خود را وارد کنید.")]
        [StringLength(
            100,
            MinimumLength = 2,
            ErrorMessage = "نام باید بین ۲ تا ۱۰۰ کاراکتر باشد.")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "ایمیل خود را وارد کنید.")]
        [EmailAddress(ErrorMessage = "ایمیل معتبر نیست.")]
        [StringLength(
            254,
            ErrorMessage = "ایمیل نمی‌تواند بیشتر از ۲۵۴ کاراکتر باشد.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "شماره تماس را وارد کنید.")]
        [RegularExpression(
            @"\A09[0-9]{9}\z",
            ErrorMessage = "شماره موبایل باید با 09 شروع شود و دقیقاً ۱۱ رقم باشد.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "عنوان پروژه را وارد کنید.")]
        [StringLength(
            150,
            MinimumLength = 3,
            ErrorMessage = "عنوان پروژه باید بین ۳ تا ۱۵۰ کاراکتر باشد.")]
        public string ProjectName { get; set; } = string.Empty;

        [Required(ErrorMessage = "نوع پروژه را انتخاب کنید.")]
        [StringLength(80)]
        public string ProjectType { get; set; } = string.Empty;

        [Required(ErrorMessage = "توضیحات پروژه را بنویسید.")]
        [StringLength(
            5000,
            MinimumLength = 20,
            ErrorMessage = "توضیحات باید بین ۲۰ تا ۵۰۰۰ کاراکتر باشد.")]
        public string Description { get; set; } = string.Empty;

        [StringLength(80)]
        public string? Budget { get; set; }

        public DateTime? DesiredDeadline { get; set; }
    }
}
