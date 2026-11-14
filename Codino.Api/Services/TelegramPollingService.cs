
using Codino.Api.Data;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

namespace Codino.Api.Services
{
    public sealed class TelegramPollingService : BackgroundService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TelegramPollingService> _logger;

        private string? _token;
        private long _adminUserId;

        public TelegramPollingService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IServiceScopeFactory scopeFactory,
            ILogger<TelegramPollingService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            _token = _configuration["Telegram:BotToken"];

            if (string.IsNullOrWhiteSpace(_token))
            {
                _logger.LogError(
                    "Telegram:BotToken is missing.");

                return;
            }

            if (!long.TryParse(
                    _configuration["Telegram:AdminUserId"],
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out _adminUserId))
            {
                _logger.LogError(
                    "Telegram:AdminUserId is missing or invalid.");

                return;
            }

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(40);

            try
            {
                // Long polling با webhook هم‌زمان کار نمی‌کند.
                await CallTelegramAsync(
                    client,
                    "deleteWebhook",
                    new { drop_pending_updates = false },
                    stoppingToken);

                _logger.LogInformation(
                    "Telegram polling started.");
            }
            catch (Exception ex) when (
                ex is HttpRequestException ||
                ex is JsonException ||
                ex is InvalidOperationException)
            {
                _logger.LogError(
                    ex,
                    "Could not initialize Telegram polling.");

                return;
            }

            var offset = 0;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var updates = await GetUpdatesAsync(
                        client,
                        offset,
                        stoppingToken);

                    foreach (var update in updates.EnumerateArray())
                    {
                        if (!update.TryGetProperty(
                                "update_id",
                                out var updateIdElement))
                        {
                            continue;
                        }

                        var updateId = updateIdElement.GetInt32();

                        // هر update فقط یک بار پردازش می‌شود.
                        offset = updateId + 1;

                        if (!update.TryGetProperty(
                                "callback_query",
                                out var callback))
                        {
                            continue;
                        }

                        await ProcessCallbackAsync(
                            client,
                            callback,
                            stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex) when (
                    ex is HttpRequestException ||
                    ex is JsonException ||
                    ex is InvalidOperationException)
                {
                    _logger.LogError(
                        ex,
                        "Telegram polling failed. Retrying in 5 seconds.");

                    await Task.Delay(
                        TimeSpan.FromSeconds(5),
                        stoppingToken);
                }
            }

            _logger.LogInformation(
                "Telegram polling stopped.");
        }

        private async Task<JsonElement> GetUpdatesAsync(
            HttpClient client,
            int offset,
            CancellationToken cancellationToken)
        {
            var url =
                $"https://api.telegram.org/bot{_token}/getUpdates" +
                $"?offset={offset}&timeout=25";

            using var response = await client.GetAsync(
                url,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            await using var stream =
                await response.Content.ReadAsStreamAsync(
                    cancellationToken);

            using var document = await JsonDocument.ParseAsync(
                stream,
                cancellationToken: cancellationToken);

            var root = document.RootElement;

            if (!root.TryGetProperty("ok", out var ok) ||
                !ok.GetBoolean() ||
                !root.TryGetProperty("result", out var result))
            {
                throw new JsonException(
                    "Telegram returned an invalid getUpdates response.");
            }

            // داده را قبل از Dispose شدن document کپی می‌کنیم.
            return result.Clone();
        }

        private async Task ProcessCallbackAsync(
            HttpClient client,
            JsonElement callback,
            CancellationToken cancellationToken)
        {
            var callbackId = callback.TryGetProperty(
                "id", out var idElement)
                ? idElement.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(callbackId))
                return;

            try
            {
                var senderId = callback
                    .GetProperty("from")
                    .GetProperty("id")
                    .GetInt64();

                if (senderId != _adminUserId)
                {
                    await AnswerCallbackAsync(
                        client,
                        callbackId,
                        "اجازه انجام این عملیات را ندارید.",
                        cancellationToken);

                    _logger.LogWarning(
                        "Unauthorized Telegram callback received.");

                    return;
                }

                var data = callback.TryGetProperty(
                    "data", out var dataElement)
                    ? dataElement.GetString()
                    : null;

                var parts = data?.Split(':');

                if (parts is not { Length: 3 } ||
                    parts[0] != "project" ||
                    !int.TryParse(parts[2], out var requestId) ||
                    requestId <= 0)
                {
                    await AnswerCallbackAsync(
                        client,
                        callbackId,
                        "اطلاعات درخواست نامعتبر است.",
                        cancellationToken);

                    return;
                }

                string newStatus;

                if (parts[1] == "approve")
                {
                    newStatus = "Approved";
                }
                else if (parts[1] == "reject")
                {
                    newStatus = "Rejected";
                }
                else
                {
                    await AnswerCallbackAsync(
                        client,
                        callbackId,
                        "عملیات ناشناخته است.",
                        cancellationToken);

                    return;
                }

                string resultMessage;

                await using (var scope = _scopeFactory.CreateAsyncScope())
                {
                    var db = scope.ServiceProvider
                        .GetRequiredService<CodinoDbContext>();

                    // تغییر اتمیک: فقط درخواست Pending تغییر می‌کند.
                    var affectedRows = await db.ProjectRequests
                        .Where(x =>
                            x.Id == requestId &&
                            x.Status == "Pending")
                        .ExecuteUpdateAsync(
                            setters => setters.SetProperty(
                                x => x.Status,
                                newStatus),
                            cancellationToken);

                    if (affectedRows == 1)
                    {
                        resultMessage = newStatus == "Approved"
                            ? "درخواست پروژه با موفقیت تأیید شد. ✅"
                            : "درخواست پروژه رد شد. ❌";

                        _logger.LogInformation(
                            "Project request {RequestId} changed to {Status}.",
                            requestId,
                            newStatus);
                    }
                    else
                    {
                        var currentStatus = await db.ProjectRequests
                            .Where(x => x.Id == requestId)
                            .Select(x => x.Status)
                            .FirstOrDefaultAsync(cancellationToken);

                        resultMessage = currentStatus == null
                            ? "درخواست موردنظر پیدا نشد."
                            : $"این درخواست قبلاً تعیین‌تکلیف شده است. وضعیت فعلی: {currentStatus}";

                        _logger.LogInformation(
                            "Project request {RequestId} was not changed. Current status: {Status}.",
                            requestId,
                            currentStatus ?? "NotFound");
                    }
                }

                await AnswerCallbackAsync(
                    client,
                    callbackId,
                    resultMessage,
                    cancellationToken);

                await RemoveButtonsAsync(
                    client,
                    callback,
                    cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to process Telegram project callback.");

                try
                {
                    await AnswerCallbackAsync(
                        client,
                        callbackId,
                        "خطا هنگام پردازش درخواست. لاگ برنامه را بررسی کنید.",
                        cancellationToken);
                }
                catch (Exception answerException)
                {
                    _logger.LogWarning(
                        answerException,
                        "Could not send Telegram callback response.");
                }
            }
        }

        private async Task AnswerCallbackAsync(
            HttpClient client,
            string callbackId,
            string message,
            CancellationToken cancellationToken)
        {
            await CallTelegramAsync(
                client,
                "answerCallbackQuery",
                new
                {
                    callback_query_id = callbackId,
                    text = message,
                    show_alert = true
                },
                cancellationToken);
        }

        private async Task RemoveButtonsAsync(
            HttpClient client,
            JsonElement callback,
            CancellationToken cancellationToken)
        {
            if (!callback.TryGetProperty(
                    "message", out var message))
            {
                return;
            }

            if (!message.TryGetProperty(
                    "chat", out var chat) ||
                !chat.TryGetProperty("id", out var chatId) ||
                !message.TryGetProperty(
                    "message_id", out var messageId))
            {
                return;
            }

            await CallTelegramAsync(
                client,
                "editMessageReplyMarkup",
                new
                {
                    chat_id = chatId.GetInt64(),
                    message_id = messageId.GetInt32(),
                    reply_markup = new
                    {
                        inline_keyboard = Array.Empty<object[]>()
                    }
                },
                cancellationToken);
        }

        private async Task CallTelegramAsync(
            HttpClient client,
            string method,
            object payload,
            CancellationToken cancellationToken)
        {
            using var response = await client.PostAsJsonAsync(
                $"https://api.telegram.org/bot{_token}/{method}",
                payload,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<
                TelegramApiResponse>(
                cancellationToken: cancellationToken);

            if (result?.Ok != true)
            {
                throw new InvalidOperationException(
                    $"Telegram API method {method} failed.");
            }
        }

        private sealed class TelegramApiResponse
        {
            public bool Ok { get; set; }
        }
    }
}
