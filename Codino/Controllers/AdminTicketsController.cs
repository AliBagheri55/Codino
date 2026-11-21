
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codino.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminTicketsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AdminTicketsController> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public AdminTicketsController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<AdminTicketsController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        private HttpClient CreateClient()
        {
            var baseUrl = _configuration["ApiSettings:BaseUrl"];

            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new InvalidOperationException("ApiSettings:BaseUrl تنظیم نشده است.");

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");

            var token = HttpContext.Session.GetString("AccessToken");

            if (!string.IsNullOrWhiteSpace(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }

            return client;
        }

        private async Task<JsonElement?> ReadDataAsync(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Admin tickets API returned {StatusCode}: {Content}",
                    (int)response.StatusCode,
                    content);

                return null;
            }

            using var document = JsonDocument.Parse(content);

            if (!document.RootElement.TryGetProperty("data", out var data))
                return null;

            return data.Clone();
        }

        private StringContent CreateJsonContent(object data)
        {
            var json = JsonSerializer.Serialize(data, JsonOptions);

            return new StringContent(
                json,
                Encoding.UTF8,
                "application/json");
        }

        // GET: /AdminTickets
        public async Task<IActionResult> Index(
            string? search,
            string? status,
            string? priority,
            string? category,
            int page = 1,
            int pageSize = 15)
        {
            try
            {
                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, 100);

                var query = new List<string>
                {
                    $"page={page}",
                    $"pageSize={pageSize}"
                };

                if (!string.IsNullOrWhiteSpace(search))
                    query.Add($"search={Uri.EscapeDataString(search)}");

                if (!string.IsNullOrWhiteSpace(status))
                    query.Add($"status={Uri.EscapeDataString(status)}");

                if (!string.IsNullOrWhiteSpace(priority))
                    query.Add($"priority={Uri.EscapeDataString(priority)}");

                if (!string.IsNullOrWhiteSpace(category))
                    query.Add($"category={Uri.EscapeDataString(category)}");

                using var client = CreateClient();

                using var response = await client.GetAsync(
                    "api/admin/tickets?" + string.Join("&", query));

                var data = await ReadDataAsync(response);

                if (data == null)
                {
                    TempData["Error"] = "دریافت فهرست تیکت‌ها ناموفق بود.";
                    return View(new List<JsonElement>());
                }

                ViewBag.Search = search;
                ViewBag.Status = status;
                ViewBag.Priority = priority;
                ViewBag.Category = category;
                ViewBag.Page = page;
                ViewBag.PageSize = pageSize;

                if (data.Value.ValueKind == JsonValueKind.Array)
                    return View(data.Value.EnumerateArray().ToList());

                if (data.Value.ValueKind == JsonValueKind.Object &&
                    data.Value.TryGetProperty("items", out var items) &&
                    items.ValueKind == JsonValueKind.Array)
                {
                    return View(items.EnumerateArray().ToList());
                }

                return View(new List<JsonElement>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin tickets.");

                TempData["Error"] = "خطا در ارتباط با سرور.";
                return View(new List<JsonElement>());
            }
        }

        // GET: /AdminTickets/Details/5
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
                return BadRequest();

            try
            {
                using var client = CreateClient();

                using var response = await client.GetAsync(
                    $"api/admin/tickets/{id}");

                var data = await ReadDataAsync(response);

                if (data == null)
                {
                    TempData["Error"] = "تیکت موردنظر پیدا نشد.";
                    return RedirectToAction(nameof(Index));
                }

                return View(data.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ticket {TicketId}.", id);

                TempData["Error"] = "خطا در دریافت اطلاعات تیکت.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /AdminTickets/Reply
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(
            int id,
            string message)
        {
            if (id <= 0 || string.IsNullOrWhiteSpace(message))
            {
                TempData["Error"] = "متن پاسخ را وارد کنید.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (message.Trim().Length > 5000)
            {
                TempData["Error"] = "متن پاسخ نباید بیشتر از ۵۰۰۰ کاراکتر باشد.";
                return RedirectToAction(nameof(Details), new { id });
            }

            try
            {
                using var client = CreateClient();

                using var response = await client.PostAsync(
                    $"api/admin/tickets/{id}/reply",
                    CreateJsonContent(new { message = message.Trim() }));

                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = "ارسال پاسخ ناموفق بود.";
                }
                else
                {
                    TempData["Success"] = "پاسخ شما با موفقیت ارسال شد.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error replying to ticket {TicketId}.", id);
                TempData["Error"] = "خطا در ارسال پاسخ.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: /AdminTickets/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(
            int id,
            string status)
        {
            var allowedStatuses = new[]
            {
                "Open",
                "WaitingForAdmin",
                "WaitingForUser",
                "Closed"
            };

            if (id <= 0 || !allowedStatuses.Contains(status))
            {
                TempData["Error"] = "وضعیت انتخاب‌شده معتبر نیست.";
                return RedirectToAction(nameof(Details), new { id });
            }

            try
            {
                using var client = CreateClient();

                using var response = await client.PatchAsync(
                    $"api/admin/tickets/{id}/status",
                    CreateJsonContent(new { status }));

                TempData[response.IsSuccessStatusCode ? "Success" : "Error"] =
                    response.IsSuccessStatusCode
                        ? "وضعیت تیکت تغییر کرد."
                        : "تغییر وضعیت تیکت ناموفق بود.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ticket status {TicketId}.", id);
                TempData["Error"] = "خطا در تغییر وضعیت تیکت.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: /AdminTickets/UpdatePriority
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePriority(
            int id,
            string priority)
        {
            var allowedPriorities = new[]
            {
                "Low",
                "Normal",
                "High"
            };

            if (id <= 0 || !allowedPriorities.Contains(priority))
            {
                TempData["Error"] = "اولویت انتخاب‌شده معتبر نیست.";
                return RedirectToAction(nameof(Details), new { id });
            }

            try
            {
                using var client = CreateClient();

                using var response = await client.PatchAsync(
                    $"api/admin/tickets/{id}/priority",
                    CreateJsonContent(new { priority }));

                TempData[response.IsSuccessStatusCode ? "Success" : "Error"] =
                    response.IsSuccessStatusCode
                        ? "اولویت تیکت تغییر کرد."
                        : "تغییر اولویت ناموفق بود.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ticket priority {TicketId}.", id);
                TempData["Error"] = "خطا در تغییر اولویت تیکت.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: /AdminTickets/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return BadRequest();

            try
            {
                using var client = CreateClient();

                using var response = await client.DeleteAsync(
                    $"api/admin/tickets/{id}");

                TempData[response.IsSuccessStatusCode ? "Success" : "Error"] =
                    response.IsSuccessStatusCode
                        ? "تیکت با موفقیت حذف شد."
                        : "حذف تیکت ناموفق بود.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting ticket {TicketId}.", id);
                TempData["Error"] = "خطا در حذف تیکت.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
