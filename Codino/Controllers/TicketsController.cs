
using System.Text.Json;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Codino.Services;

namespace Codino.Controllers
{
    [Authorize(Roles = "User")]
    [Route("Tickets")]
    public class TicketsController : Controller
    {
        private readonly ApiService _apiService;
        private readonly ILogger<TicketsController> _logger;

        public TicketsController(
            ApiService apiService,
            ILogger<TicketsController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        // بررسی توکن ورود کاربر
        private bool HasAccessToken()
        {
            return !string.IsNullOrWhiteSpace(
                HttpContext.Session.GetString("AccessToken"));
        }

        // استخراج اطلاعات اصلی از پاسخ API
        private static JsonElement ExtractData(JsonElement response)
        {
            if (response.ValueKind == JsonValueKind.Object &&
                response.TryGetProperty("data", out JsonElement data))
            {
                return data;
            }

            return response;
        }

        // GET: /Tickets

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(
                HttpContext.Session.GetString("AccessToken")))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                JsonElement? result =
                    await _apiService.GetMyTicketsAsync(cancellationToken);

                var tickets = new List<JsonElement>();

                if (result.HasValue)
                {
                    JsonElement data = result.Value;

                    // استخراج اطلاعات از پاسخ استاندارد API
                    if (data.ValueKind == JsonValueKind.Object &&
                        data.TryGetProperty("data", out var responseData))
                    {
                        data = responseData;
                    }

                    if (data.ValueKind == JsonValueKind.Array)
                    {
                        tickets = data.EnumerateArray().ToList();
                    }
                }

                return View(tickets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user tickets.");

                TempData["TicketError"] =
                    "دریافت فهرست تیکت‌ها با مشکل مواجه شد.";

                return View(new List<JsonElement>());
            }
        }

        // GET: /Tickets/Create
        [HttpGet("Create")]
        public IActionResult Create()
        {
            if (!HasAccessToken())
                return RedirectToAction("Login", "Account");

            return View(new CreateTicketViewModel());
        }

        // POST: /Tickets/Create
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            CreateTicketViewModel model,
            CancellationToken cancellationToken)
        {
            if (!HasAccessToken())
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                JsonElement? result =
                    await _apiService.CreateTicketAsync(
                        model.Subject,
                        model.Category,
                        model.Priority,
                        model.Message,
                        cancellationToken);

                if (result == null)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "ثبت تیکت انجام نشد. لطفاً دوباره تلاش کنید.");

                    return View(model);
                }

                JsonElement data = ExtractData(result.Value);

                if (TryGetInt32(data, "id", out int ticketId) ||
                    TryGetInt32(data, "Id", out ticketId))
                {
                    TempData["Success"] =
                        "تیکت شما با موفقیت ثبت شد.";

                    return RedirectToAction(
                        nameof(Details),
                        new { id = ticketId });
                }

                _logger.LogWarning(
                    "Ticket creation response did not contain a valid ticket ID.");

                ModelState.AddModelError(
                    string.Empty,
                    "تیکت ثبت شد، اما دریافت شناسه آن با مشکل مواجه شد.");

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error creating support ticket.");

                ModelState.AddModelError(
                    string.Empty,
                    "هنگام ثبت تیکت خطایی رخ داد.");

                return View(model);
            }
        }

        // GET: /Tickets/Details/5
        [HttpGet("Details/{id:int}")]
        public async Task<IActionResult> Details(
            int id,
            CancellationToken cancellationToken)
        {
            if (!HasAccessToken())
                return RedirectToAction("Login", "Account");

            if (id <= 0)
                return NotFound();

            try
            {
                JsonElement? result =
                    await _apiService.GetTicketDetailsAsync(
                        id,
                        cancellationToken);

                if (result == null)
                {
                    _logger.LogWarning(
                        "Ticket {TicketId} was not found or API returned null.",
                        id);

                    return NotFound();
                }

                JsonElement ticket = ExtractData(result.Value);

                if (ticket.ValueKind != JsonValueKind.Object)
                {
                    _logger.LogWarning(
                        "Invalid ticket response for ticket {TicketId}. ValueKind: {ValueKind}",
                        id,
                        ticket.ValueKind);

                    return NotFound();
                }

                if (!TryGetInt32(ticket, "id", out _) &&
                    !TryGetInt32(ticket, "Id", out _))
                {
                    _logger.LogWarning(
                        "Ticket response does not contain an ID. TicketId: {TicketId}",
                        id);

                    return NotFound();
                }

                return View(ticket);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error loading ticket details. TicketId: {TicketId}",
                    id);

                TempData["TicketError"] =
                    "دریافت اطلاعات تیکت با مشکل مواجه شد.";

                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /Tickets/SendMessage
        [HttpPost("SendMessage")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(
            SendTicketMessageViewModel model,
            CancellationToken cancellationToken)
        {
            if (!HasAccessToken())
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                TempData["TicketError"] =
                    "متن پیام باید بین ۱۰ تا ۵۰۰۰ کاراکتر باشد.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = model.TicketId });
            }

            try
            {
                JsonElement? result =
                    await _apiService.SendTicketMessageAsync(
                        model.TicketId,
                        model.Message,
                        cancellationToken);

                if (result == null)
                {
                    TempData["TicketError"] =
                        "ارسال پیام انجام نشد. لطفاً دوباره تلاش کنید.";
                }
                else
                {
                    TempData["Success"] =
                        "پیام شما با موفقیت ارسال شد.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error sending ticket message. TicketId: {TicketId}",
                    model.TicketId);

                TempData["TicketError"] =
                    "هنگام ارسال پیام خطایی رخ داد.";
            }

            return RedirectToAction(
                nameof(Details),
                new { id = model.TicketId });
        }

        // استخراج عدد از پاسخ JSON
        private static bool TryGetInt32(
            JsonElement element,
            string propertyName,
            out int value)
        {
            value = 0;

            return element.ValueKind == JsonValueKind.Object &&
                   element.TryGetProperty(
                       propertyName,
                       out JsonElement property) &&
                   property.TryGetInt32(out value);
        }
    }

    public class CreateTicketViewModel
    {
        [Required(ErrorMessage = "موضوع تیکت الزامی است.")]
        [StringLength(
            150,
            MinimumLength = 3,
            ErrorMessage = "موضوع باید بین ۳ تا ۱۵۰ کاراکتر باشد.")]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Category { get; set; } = "General";

        [Required]
        public string Priority { get; set; } = "Normal";

        [Required(ErrorMessage = "متن پیام الزامی است.")]
        [StringLength(
            5000,
            MinimumLength = 10,
            ErrorMessage = "پیام باید بین ۱۰ تا ۵۰۰۰ کاراکتر باشد.")]
        public string Message { get; set; } = string.Empty;
    }

    public class SendTicketMessageViewModel
    {
        [Range(1, int.MaxValue)]
        public int TicketId { get; set; }

        [Required(ErrorMessage = "متن پیام الزامی است.")]
        [StringLength(
            5000,
            MinimumLength = 10,
            ErrorMessage = "پیام باید بین ۱۰ تا ۵۰۰۰ کاراکتر باشد.")]
        public string Message { get; set; } = string.Empty;
    }
}
