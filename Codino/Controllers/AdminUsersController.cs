
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codino.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminUsersController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web);

        public AdminUsersController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        // =====================================================
        // ساخت HttpClient و ارسال توکن مدیر به API
        // =====================================================

        private HttpClient CreateApiClient()
        {
            string? baseUrl =
                _configuration["ApiSettings:BaseUrl"];

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new InvalidOperationException(
                    "آدرس API در تنظیمات پروژه تعریف نشده است.");
            }

            string? accessToken =
                HttpContext.Session.GetString("AccessToken");

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw new UnauthorizedAccessException();
            }

            var client =
                _httpClientFactory.CreateClient();

            client.BaseAddress =
                new Uri(baseUrl.TrimEnd('/') + "/");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    accessToken);

            return client;
        }

        // =====================================================
        // INDEX
        // نمایش فهرست کاربران
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Index(
            string? search,
            int page = 1)
        {
            try
            {
                page = Math.Max(1, page);

                using var client = CreateApiClient();

                string url =
                    $"api/admin/users?page={page}&pageSize=10";

                if (!string.IsNullOrWhiteSpace(search))
                {
                    url +=
                        $"&search={Uri.EscapeDataString(search.Trim())}";
                }

                var response =
                    await client.GetAsync(url);

                if (response.StatusCode == HttpStatusCode.Unauthorized ||
                    response.StatusCode == HttpStatusCode.Forbidden)
                {
                    return RedirectToAction(
                        "AccessDenied",
                        "Account");
                }

                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] =
                        await ReadApiError(response);

                    return View(new AdminUsersPageViewModel());
                }

                var result =
                    await response.Content
                        .ReadFromJsonAsync<ApiUsersResponse>(
                            JsonOptions);

                if (result?.Data == null)
                {
                    TempData["Error"] =
                        "اطلاعات کاربران دریافت نشد.";

                    return View(new AdminUsersPageViewModel());
                }

                var model = new AdminUsersPageViewModel
                {
                    Users = result.Data,
                    Pagination = result.Pagination ??
                        new UsersPaginationViewModel(),

                    Search = search ?? string.Empty
                };

                return View(model);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }
            catch (Exception)
            {
                TempData["Error"] =
                    "در دریافت اطلاعات کاربران مشکلی رخ داد.";

                return View(new AdminUsersPageViewModel());
            }
        }

        // =====================================================
        // DETAILS
        // مشاهده جزئیات کاربر
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
            {
                return RedirectToAction(nameof(Index));
            }

            try
            {
                using var client = CreateApiClient();

                var response =
                    await client.GetAsync(
                        $"api/admin/users/{id}");

                if (response.StatusCode == HttpStatusCode.Unauthorized ||
                    response.StatusCode == HttpStatusCode.Forbidden)
                {
                    return RedirectToAction(
                        "AccessDenied",
                        "Account");
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    TempData["Error"] =
                        "کاربر موردنظر پیدا نشد.";

                    return RedirectToAction(nameof(Index));
                }

                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] =
                        await ReadApiError(response);

                    return RedirectToAction(nameof(Index));
                }

                var result =
                    await response.Content
                        .ReadFromJsonAsync<ApiUserResponse>(
                            JsonOptions);

                if (result?.Data == null)
                {
                    TempData["Error"] =
                        "اطلاعات کاربر دریافت نشد.";

                    return RedirectToAction(nameof(Index));
                }

                return View(result.Data);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }
            catch (Exception)
            {
                TempData["Error"] =
                    "در دریافت جزئیات کاربر مشکلی رخ داد.";

                return RedirectToAction(nameof(Index));
            }
        }

        // =====================================================
        // DELETE
        // حذف کاربر
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
            {
                TempData["Error"] =
                    "شناسه کاربر معتبر نیست.";

                return RedirectToAction(nameof(Index));
            }

            try
            {
                using var client = CreateApiClient();

                var response =
                    await client.DeleteAsync(
                        $"api/admin/users/{id}");

                if (response.StatusCode == HttpStatusCode.Unauthorized ||
                    response.StatusCode == HttpStatusCode.Forbidden)
                {
                    return RedirectToAction(
                        "AccessDenied",
                        "Account");
                }

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] =
                        "کاربر با موفقیت حذف شد.";
                }
                else
                {
                    TempData["Error"] =
                        await ReadApiError(response);
                }
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }
            catch (Exception)
            {
                TempData["Error"] =
                    "در حذف کاربر مشکلی رخ داد.";
            }

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // خواندن پیام خطای API
        // =====================================================

        private static async Task<string> ReadApiError(
            HttpResponseMessage response)
        {
            try
            {
                string content =
                    await response.Content.ReadAsStringAsync();

                using var document =
                    JsonDocument.Parse(content);

                if (document.RootElement.TryGetProperty(
                    "message", out var message))
                {
                    return message.GetString() ??
                        "درخواست با خطا مواجه شد.";
                }
            }
            catch
            {
                // در صورت نامعتبر بودن پاسخ، پیام عمومی نمایش داده می‌شود.
            }

            return "ارتباط با سرویس مدیریت کاربران با خطا مواجه شد.";
        }
    }

    // =========================================================
    // مدل پاسخ فهرست کاربران API
    // =========================================================

    public class ApiUsersResponse
    {
        public bool Success { get; set; }

        public List<AdminUserViewModel> Data { get; set; } = new();

        public UsersPaginationViewModel? Pagination { get; set; }
    }

    // =========================================================
    // مدل پاسخ جزئیات کاربر API
    // =========================================================

    public class ApiUserResponse
    {
        public bool Success { get; set; }

        public AdminUserViewModel? Data { get; set; }
    }

    // =========================================================
    // اطلاعات قابل نمایش کاربر
    // =========================================================

    public class AdminUserViewModel
    {
        public int Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string Role { get; set; } = "User";

        public DateTime CreatedAt { get; set; }

        public bool IsPhoneVerified { get; set; }
    }

    // =========================================================
    // اطلاعات صفحه‌بندی
    // =========================================================

    public class UsersPaginationViewModel
    {
        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        public int TotalCount { get; set; }

        public int TotalPages { get; set; }
    }

    // =========================================================
    // مدل صفحه مدیریت کاربران
    // =========================================================

    public class AdminUsersPageViewModel
    {
        public List<AdminUserViewModel> Users { get; set; } = new();

        public UsersPaginationViewModel Pagination { get; set; } =
            new();

        public string Search { get; set; } = string.Empty;
    }
}
