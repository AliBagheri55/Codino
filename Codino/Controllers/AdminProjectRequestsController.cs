
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codino.Controllers
{
    // فقط مدیر سایت اجازه مدیریت درخواست‌های پروژه را دارد.
    [Authorize(Roles = "Admin")]
    public class AdminProjectRequestsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public AdminProjectRequestsController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        // دریافت آدرس API و توکن مدیر
        private HttpClient CreateApiClient()
        {
            var baseUrl = _configuration["ApiSettings:BaseUrl"];
            var token = HttpContext.Session.GetString("AccessToken");

            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new InvalidOperationException("آدرس API تنظیم نشده است.");

            if (string.IsNullOrWhiteSpace(token))
                throw new UnauthorizedAccessException("توکن ورود مدیر پیدا نشد.");

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            return client;
        }

        // نمایش فهرست درخواست‌ها
        [HttpGet]
        public async Task<IActionResult> Index(
            string? search,
            string? status,
            int page = 1)
        {
            try
            {
                using var client = CreateApiClient();

                var query = new List<string>
                {
                    $"page={Math.Max(page, 1)}",
                    "pageSize=10"
                };

                if (!string.IsNullOrWhiteSpace(search))
                    query.Add($"search={Uri.EscapeDataString(search.Trim())}");

                if (!string.IsNullOrWhiteSpace(status))
                    query.Add($"status={Uri.EscapeDataString(status.Trim())}");

                var url = "api/admin/project-requests?" +
                          string.Join("&", query);

                var response = await client.GetAsync(url);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                    response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    return RedirectToAction("AccessDenied", "Account");
                }

                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<
                    ApiListResponse<ProjectRequestViewModel>>(JsonOptions);

                var model = new ProjectRequestsPageViewModel
                {
                    Requests = result?.Data ?? new(),
                    Pagination = result?.Pagination ?? new(),
                    Search = search,
                    Status = status
                };

                ViewData["Title"] = "مدیریت درخواست‌های پروژه";
                ViewData["PageTitle"] = "درخواست‌های پروژه";

                return View(model);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account");
            }
            catch (Exception)
            {
                TempData["Error"] = "دریافت درخواست‌های پروژه با مشکل مواجه شد.";

                return View(new ProjectRequestsPageViewModel
                {
                    Requests = new(),
                    Pagination = new(),
                    Search = search,
                    Status = status
                });
            }
        }

        // نمایش جزئیات یک درخواست
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
                return BadRequest();

            try
            {
                using var client = CreateApiClient();

                var response = await client.GetAsync(
                    $"api/admin/project-requests/{id}");

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return NotFound();

                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<
                    ApiSingleResponse<ProjectRequestViewModel>>(JsonOptions);

                if (result?.Data == null)
                    return NotFound();

                ViewData["Title"] = "جزئیات درخواست پروژه";
                ViewData["PageTitle"] = "جزئیات درخواست";

                return View(result.Data);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account");
            }
            catch
            {
                TempData["Error"] = "دریافت جزئیات درخواست ناموفق بود.";
                return RedirectToAction(nameof(Index));
            }
        }

        // تغییر وضعیت درخواست
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(
            int id,
            string status)
        {
            if (id <= 0 ||
                string.IsNullOrWhiteSpace(status) ||
                !new[] { "Pending", "Approved", "Rejected" }.Contains(status))
            {
                TempData["Error"] = "اطلاعات وضعیت معتبر نیست.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                using var client = CreateApiClient();

                var response = await client.PatchAsJsonAsync(
                    $"api/admin/project-requests/{id}/status",
                    new { status });

                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = "تغییر وضعیت درخواست انجام نشد.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                TempData["Success"] = "وضعیت درخواست با موفقیت تغییر کرد.";
            }
            catch
            {
                TempData["Error"] = "ارتباط با سرور برقرار نشد.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // حذف درخواست پروژه
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return BadRequest();

            try
            {
                using var client = CreateApiClient();

                var response = await client.DeleteAsync(
                    $"api/admin/project-requests/{id}");

                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = "حذف درخواست انجام نشد.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                TempData["Success"] = "درخواست پروژه حذف شد.";
            }
            catch
            {
                TempData["Error"] = "ارتباط با سرور برقرار نشد.";
                return RedirectToAction(nameof(Details), new { id });
            }

            return RedirectToAction(nameof(Index));
        }
    }

    // مدل پاسخ فهرست API
    public class ApiListResponse<T>
    {
        public bool Success { get; set; }
        public List<T> Data { get; set; } = new();
        public PaginationViewModel Pagination { get; set; } = new();
    }

    // مدل پاسخ تکی API
    public class ApiSingleResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
    }

    // اطلاعات هر درخواست پروژه
    public class ProjectRequestViewModel
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string CustomerName { get; set; } = "";
        public string Email { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string ProjectName { get; set; } = "";
        public string ProjectType { get; set; } = "";
        public string Description { get; set; } = "";
        public string? Budget { get; set; }
        public DateTime? DesiredDeadline { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAtUtc { get; set; }
    }

    // اطلاعات صفحه‌بندی
    public class PaginationViewModel
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }

    // مدل صفحه فهرست درخواست‌ها
    public class ProjectRequestsPageViewModel
    {
        public List<ProjectRequestViewModel> Requests { get; set; } = new();
        public PaginationViewModel Pagination { get; set; } = new();
        public string? Search { get; set; }
        public string? Status { get; set; }
    }
}
