using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Codino.Models.Api;
using Microsoft.AspNetCore.Http;

namespace Codino.Services
{
    /// <summary>
    /// مدیریت ارتباط برنامه MVC کدینو با API.
    /// </summary>
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ApiService> _logger;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ApiService(
      HttpClient httpClient,
      IHttpContextAccessor httpContextAccessor,
      ILogger<ApiService> logger)
        {
            _httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));

            _httpContextAccessor = httpContextAccessor
                ?? throw new ArgumentNullException(nameof(httpContextAccessor));

            _logger = logger
                ?? throw new ArgumentNullException(nameof(logger));
        }

        // =========================================================
        // ACCESS TOKEN
        // =========================================================
        private string? GetAccessToken()
        {
            try
            {
                HttpContext? httpContext =
                    _httpContextAccessor.HttpContext;

                if (httpContext == null)
                {
                    LogException(
                        "GET ACCESS TOKEN",
                        new InvalidOperationException(
                            "HttpContext is not available."));

                    return null;
                }

                string? accessToken =
                    httpContext.Session.GetString("AccessToken");

                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    // توکن وجود ندارد؛ درخواست احراز هویت‌شده
                    // نباید با توکن خالی ارسال شود.
                    return null;
                }

                return accessToken.Trim();
            }
            catch (Exception ex)
            {
                LogException("GET ACCESS TOKEN", ex);
                return null;
            }
        }

        private HttpRequestMessage CreateAuthenticatedRequest(
            HttpMethod method,
            string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentException(
                    "Request URL cannot be empty.",
                    nameof(url));
            }

            HttpRequestMessage request =
                new(method, url);

            try
            {
                string? accessToken = GetAccessToken();

                if (!string.IsNullOrWhiteSpace(accessToken))
                {
                    request.Headers.Authorization =
                        new AuthenticationHeaderValue(
                            "Bearer",
                            accessToken);
                }

                return request;
            }
            catch
            {
                request.Dispose();
                throw;
            }
        }

        // =========================================================
        // LOGIN
        // =========================================================

        public async Task<LoginResponse> LoginAsync(
            LoginRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = "اطلاعات ورود ارسال نشده است."
                };
            }

            try
            {
                using HttpResponseMessage response =
                    await _httpClient.PostAsJsonAsync(
                        "api/Auth/login",
                        request,
                        JsonOptions,
                        cancellationToken);

                string content = await response.Content.ReadAsStringAsync(
                    cancellationToken);

                LogResponse("LOGIN", response, content);

                if (!response.IsSuccessStatusCode)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = GetErrorMessageFromContent(
                            content,
                            GetDefaultHttpErrorMessage(response.StatusCode))
                    };
                }

                if (string.IsNullOrWhiteSpace(content))
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = GetEmptyResponseMessage(
                            response.StatusCode,
                            "ورود")
                    };
                }

                return Deserialize<LoginResponse>(content)
                    ?? new LoginResponse
                    {
                        Success = false,
                        Message = "پاسخ دریافتی برای ورود معتبر نیست."
                    };
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (TaskCanceledException ex)
            {
                LogException("LOGIN TIMEOUT", ex);

                return new LoginResponse
                {
                    Success = false,
                    Message = "زمان پاسخ سرور به پایان رسید."
                };
            }
            catch (HttpRequestException ex)
            {
                LogException("LOGIN HTTP ERROR", ex);

                return new LoginResponse
                {
                    Success = false,
                    Message = "ارتباط با سرور برقرار نشد."
                };
            }
            catch (Exception ex)
            {
                LogException("LOGIN ERROR", ex);

                return new LoginResponse
                {
                    Success = false,
                    Message = "خطای غیرمنتظره‌ای هنگام ورود رخ داد."
                };
            }
        }

        // =========================================================
        // REGISTER
        // =========================================================

        public async Task<ApiResponse> RegisterAsync(
            RegisterRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                return Failure("اطلاعات ثبت‌نام ارسال نشده است.");
            }

            try
            {
                using HttpResponseMessage response =
                    await _httpClient.PostAsJsonAsync(
                        "api/Auth/register",
                        request,
                        JsonOptions,
                        cancellationToken);

                string content = await response.Content.ReadAsStringAsync(
                    cancellationToken);

                LogResponse("REGISTER", response, content);

                if (response.StatusCode == HttpStatusCode.NoContent)
                {
                    return Success("ثبت‌نام با موفقیت انجام شد.");
                }

                return ParseApiResponse(
                    response,
                    content,
                    "پاسخ ثبت‌نام از API قابل پردازش نیست.");
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (TaskCanceledException ex)
            {
                LogException("REGISTER TIMEOUT", ex);
                return Failure("زمان پاسخ سرور به پایان رسید.");
            }
            catch (HttpRequestException ex)
            {
                LogException("REGISTER HTTP ERROR", ex);
                return Failure("ارتباط با API برقرار نشد.");
            }
            catch (Exception ex)
            {
                LogException("REGISTER ERROR", ex);
                return Failure("خطایی هنگام ثبت‌نام رخ داد.");
            }
        }

        // =========================================================
        // VERIFY REGISTER
        // =========================================================

        public async Task<ApiResponse> VerifyRegisterAsync(
            string phoneNumber,
            string code,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return Failure("شماره موبایل وارد نشده است.");
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                return Failure("کد تأیید وارد نشده است.");
            }

            phoneNumber = phoneNumber.Trim();
            code = code.Trim();

            if (code.Length != 4 || !code.All(char.IsDigit))
            {
                return Failure("کد تأیید باید ۴ رقمی باشد.");
            }

            VerifyRegisterRequest request = new()
            {
                PhoneNumber = phoneNumber,
                Code = code
            };

            try
            {
                using HttpResponseMessage response =
                    await _httpClient.PostAsJsonAsync(
                        "api/Auth/verify-register",
                        request,
                        JsonOptions,
                        cancellationToken);

                string content = await response.Content.ReadAsStringAsync(
                    cancellationToken);

                LogResponse("VERIFY REGISTER", response, content);

                return ParseApiResponse(
                    response,
                    content,
                    "پاسخ تأیید ثبت‌نام نامعتبر است.");
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (TaskCanceledException ex)
            {
                LogException("VERIFY REGISTER TIMEOUT", ex);
                return Failure("زمان پاسخ سرور به پایان رسید.");
            }
            catch (HttpRequestException ex)
            {
                LogException("VERIFY REGISTER HTTP ERROR", ex);
                return Failure("ارتباط با API برقرار نشد.");
            }
            catch (Exception ex)
            {
                LogException("VERIFY REGISTER ERROR", ex);
                return Failure("خطایی هنگام تأیید ثبت‌نام رخ داد.");
            }
        }

        // =========================================================
        // CAPTCHA
        // =========================================================

        public async Task<CaptchaResponse> GetCaptchaAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                using HttpResponseMessage response =
                    await _httpClient.GetAsync(
                        "api/Auth/captcha",
                        cancellationToken);

                string content = await response.Content.ReadAsStringAsync(
                    cancellationToken);

                LogResponse("CAPTCHA", response, content);

                if (!response.IsSuccessStatusCode)
                {
                    return new CaptchaResponse
                    {
                        Success = false,
                        Message = GetErrorMessageFromContent(
                            content,
                            GetDefaultHttpErrorMessage(response.StatusCode))
                    };
                }

                if (string.IsNullOrWhiteSpace(content))
                {
                    return new CaptchaResponse
                    {
                        Success = false,
                        Message = GetEmptyResponseMessage(
                            response.StatusCode,
                            "کپچا")
                    };
                }

                return Deserialize<CaptchaResponse>(content)
                    ?? new CaptchaResponse
                    {
                        Success = false,
                        Message = "پاسخ کپچا نامعتبر است."
                    };
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (TaskCanceledException ex)
            {
                LogException("CAPTCHA TIMEOUT", ex);

                return new CaptchaResponse
                {
                    Success = false,
                    Message = "زمان دریافت کپچا به پایان رسید."
                };
            }
            catch (HttpRequestException ex)
            {
                LogException("CAPTCHA HTTP ERROR", ex);

                return new CaptchaResponse
                {
                    Success = false,
                    Message = "ارتباط با API برقرار نشد."
                };
            }
            catch (Exception ex)
            {
                LogException("CAPTCHA ERROR", ex);

                return new CaptchaResponse
                {
                    Success = false,
                    Message = "خطایی هنگام دریافت کپچا رخ داد."
                };
            }
        }

        // =========================================================
        // FORGOT PASSWORD
        // =========================================================

        public async Task<ForgotPasswordResponse> ForgotPasswordAsync(
            ForgotPasswordRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                return new ForgotPasswordResponse
                {
                    Success = false,
                    Message = "اطلاعات بازیابی رمز عبور ارسال نشده است."
                };
            }

            try
            {
                using HttpResponseMessage response =
                    await _httpClient.PostAsJsonAsync(
                        "api/Auth/forgot-password",
                        request,
                        JsonOptions,
                        cancellationToken);

                string content = await response.Content.ReadAsStringAsync(
                    cancellationToken);

                LogResponse("FORGOT PASSWORD", response, content);

                if (!response.IsSuccessStatusCode)
                {
                    return new ForgotPasswordResponse
                    {
                        Success = false,
                        Message = GetErrorMessageFromContent(
                            content,
                            GetDefaultHttpErrorMessage(response.StatusCode))
                    };
                }

                if (string.IsNullOrWhiteSpace(content))
                {
                    return new ForgotPasswordResponse
                    {
                        Success = false,
                        Message = GetEmptyResponseMessage(
                            response.StatusCode,
                            "بازیابی رمز عبور")
                    };
                }

                return Deserialize<ForgotPasswordResponse>(content)
                    ?? new ForgotPasswordResponse
                    {
                        Success = false,
                        Message = "پاسخ بازیابی رمز عبور نامعتبر است."
                    };
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (TaskCanceledException ex)
            {
                LogException("FORGOT PASSWORD TIMEOUT", ex);

                return new ForgotPasswordResponse
                {
                    Success = false,
                    Message = "زمان پاسخ سرور به پایان رسید."
                };
            }
            catch (HttpRequestException ex)
            {
                LogException("FORGOT PASSWORD HTTP ERROR", ex);

                return new ForgotPasswordResponse
                {
                    Success = false,
                    Message = "ارتباط با API برقرار نشد."
                };
            }
            catch (Exception ex)
            {
                LogException("FORGOT PASSWORD ERROR", ex);

                return new ForgotPasswordResponse
                {
                    Success = false,
                    Message = "خطایی هنگام بازیابی رمز عبور رخ داد."
                };
            }
        }

        // =========================================================
        // RESET PASSWORD
        // =========================================================

        public async Task<ApiResponse> ResetPasswordAsync(
            ResetPasswordRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                return Failure("اطلاعات تغییر رمز عبور ارسال نشده است.");
            }

            try
            {
                using HttpResponseMessage response =
                    await _httpClient.PostAsJsonAsync(
                        "api/Auth/reset-password",
                        request,
                        JsonOptions,
                        cancellationToken);

                string content = await response.Content.ReadAsStringAsync(
                    cancellationToken);

                LogResponse("RESET PASSWORD", response, content);

                return ParseApiResponse(
                    response,
                    content,
                    "پاسخ تغییر رمز عبور نامعتبر است.");
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (TaskCanceledException ex)
            {
                LogException("RESET PASSWORD TIMEOUT", ex);
                return Failure("زمان پاسخ سرور به پایان رسید.");
            }
            catch (HttpRequestException ex)
            {
                LogException("RESET PASSWORD HTTP ERROR", ex);
                return Failure("ارتباط با API برقرار نشد.");
            }
            catch (Exception ex)
            {
                LogException("RESET PASSWORD ERROR", ex);
                return Failure("خطایی هنگام تغییر رمز عبور رخ داد.");
            }
        }

        // =========================================================
        // GET COMMENTS
        // =========================================================
public async Task<List<CommentResponse>> GetCommentsAsync(
    CancellationToken cancellationToken = default)
        {
            try
            {
                using HttpResponseMessage response =
                    await _httpClient.GetAsync(
                        "api/Comments",
                        cancellationToken);

                string content =
                    await response.Content.ReadAsStringAsync(
                        cancellationToken);

                _logger.LogInformation(
                    "GET COMMENTS: Status={StatusCode}, BaseAddress={BaseAddress}, ContentLength={Length}",
                    (int)response.StatusCode,
                    _httpClient.BaseAddress,
                    content.Length);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "GET COMMENTS failed. Status={StatusCode}, Reason={Reason}",
                        (int)response.StatusCode,
                        response.ReasonPhrase);

                    return new List<CommentResponse>();
                }

                if (string.IsNullOrWhiteSpace(content))
                {
                    _logger.LogWarning(
                        "GET COMMENTS returned an empty response.");

                    return new List<CommentResponse>();
                }

                List<CommentResponse>? comments;

                try
                {
                    comments =
                        JsonSerializer.Deserialize<List<CommentResponse>>(
                            content,
                            JsonOptions);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(
                        ex,
                        "GET COMMENTS JSON format is not a list of comments.");

                    return new List<CommentResponse>();
                }

                if (comments == null)
                {
                    _logger.LogWarning(
                        "GET COMMENTS deserialization returned null.");

                    return new List<CommentResponse>();
                }

                // در این مرحله نظرات را بر اساس امتیاز یا متن حذف نمی‌کنیم.
                // ابتدا باید مطمئن شویم API اطلاعات را درست برمی‌گرداند.
                List<CommentResponse> validComments =
                    comments
                        .Where(comment => comment != null)
                        .OrderByDescending(comment => comment.CreatedAt)
                        .ToList();

                _logger.LogInformation(
                    "GET COMMENTS completed. Received={Received}, Returned={Returned}",
                    comments.Count,
                    validComments.Count);

                return validComments;
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "GET COMMENTS HTTP request failed. BaseAddress={BaseAddress}",
                    _httpClient.BaseAddress);

                return new List<CommentResponse>();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while getting comments.");

                return new List<CommentResponse>();
            }
        }
        // =========================================================
        // CREATE COMMENT
        // =========================================================

        public async Task<ApiResponse> CreateCommentAsync(
            CreateCommentRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                return Failure("اطلاعات نظر ارسال نشده است.");
            }

            if (request.UserId <= 0)
            {
                return Failure("شناسه کاربر معتبر نیست.");
            }

            if (request.Rating < 1 || request.Rating > 5)
            {
                return Failure("امتیاز باید بین ۱ تا ۵ باشد.");
            }

            request.Text = request.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return Failure("متن نظر را وارد کنید.");
            }

            if (request.Text.Length > 500)
            {
                return Failure("متن نظر نمی‌تواند بیشتر از ۵۰۰ کاراکتر باشد.");
            }

            string? accessToken = GetAccessToken();

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return Failure("برای ثبت نظر باید وارد حساب کاربری شوید.");
            }

            try
            {
                using HttpRequestMessage httpRequest =
                    CreateAuthenticatedRequest(
                        HttpMethod.Post,
                        "api/Comments");

                httpRequest.Content = JsonContent.Create(
                    request,
                    options: JsonOptions);

                using HttpResponseMessage response =
                    await _httpClient.SendAsync(
                        httpRequest,
                        cancellationToken);

                string content = await response.Content.ReadAsStringAsync(
                    cancellationToken);

                LogResponse("CREATE COMMENT", response, content);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return Failure(
                        "نشست شما منقضی شده است. دوباره وارد حساب شوید.");
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    return Failure("شما اجازه ثبت نظر ندارید.");
                }

                return ParseApiResponse(
                    response,
                    content,
                    "پاسخ ثبت نظر نامعتبر است.");
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (TaskCanceledException ex)
            {
                LogException("CREATE COMMENT TIMEOUT", ex);
                return Failure("زمان پاسخ سرور به پایان رسید.");
            }
            catch (HttpRequestException ex)
            {
                LogException("CREATE COMMENT HTTP ERROR", ex);
                return Failure("ارتباط با API برقرار نشد.");
            }
            catch (Exception ex)
            {
                LogException("CREATE COMMENT ERROR", ex);
                return Failure("خطایی هنگام ثبت نظر رخ داد.");
            }
        }
// =========================================================
// GET COURSES
// =========================================================
// =========================================================
// GET COURSES
// =========================================================


        // =========================================================
        // PARSE API RESPONSE
        // =========================================================

        private static ApiResponse ParseApiResponse(
            HttpResponseMessage response,
            string content,
            string invalidResponseMessage)
        {
            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return response.IsSuccessStatusCode
                    ? Success("عملیات با موفقیت انجام شد.")
                    : Failure(GetDefaultHttpErrorMessage(response.StatusCode));
            }

            if (!response.IsSuccessStatusCode)
            {
                return Failure(
                    GetErrorMessageFromContent(
                        content,
                        GetDefaultHttpErrorMessage(response.StatusCode)));
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return Failure(
                    GetEmptyResponseMessage(
                        response.StatusCode,
                        "درخواست"));
            }

            return Deserialize<ApiResponse>(content)
                ?? Failure(invalidResponseMessage);
        }

        // =========================================================
        // SAFE JSON DESERIALIZATION
        // =========================================================

        private static T? Deserialize<T>(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return default;
            }

            try
            {
                return JsonSerializer.Deserialize<T>(
                    content,
                    JsonOptions);
            }
            catch (JsonException ex)
            {
                LogException("JSON DESERIALIZATION ERROR", ex);
                return default;
            }
        }

        // =========================================================
        // ERROR MESSAGE FROM CONTENT
        // =========================================================

        private static string GetErrorMessageFromContent(
            string content,
            string defaultMessage)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return defaultMessage;
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(content);
                JsonElement root = document.RootElement;

                string[] messageProperties =
                {
                    "message",
                    "title",
                    "detail"
                };

                foreach (string propertyName in messageProperties)
                {
                    if (TryGetPropertyIgnoreCase(
                        root,
                        propertyName,
                        out JsonElement value) &&
                        value.ValueKind == JsonValueKind.String)
                    {
                        string? message = value.GetString();

                        if (!string.IsNullOrWhiteSpace(message))
                        {
                            return message;
                        }
                    }
                }

                if (TryGetPropertyIgnoreCase(
                    root,
                    "errors",
                    out JsonElement errors))
                {
                    if (errors.ValueKind == JsonValueKind.Object)
                    {
                        foreach (JsonProperty property in errors.EnumerateObject())
                        {
                            if (property.Value.ValueKind == JsonValueKind.Array)
                            {
                                foreach (JsonElement error in
                                    property.Value.EnumerateArray())
                                {
                                    if (error.ValueKind == JsonValueKind.String)
                                    {
                                        string? message = error.GetString();

                                        if (!string.IsNullOrWhiteSpace(message))
                                        {
                                            return message;
                                        }
                                    }
                                }
                            }
                            else if (property.Value.ValueKind ==
                                     JsonValueKind.String)
                            {
                                string? message = property.Value.GetString();

                                if (!string.IsNullOrWhiteSpace(message))
                                {
                                    return message;
                                }
                            }
                        }
                    }
                    else if (errors.ValueKind == JsonValueKind.Array)
                    {
                        foreach (JsonElement error in errors.EnumerateArray())
                        {
                            if (error.ValueKind == JsonValueKind.String)
                            {
                                string? message = error.GetString();

                                if (!string.IsNullOrWhiteSpace(message))
                                {
                                    return message;
                                }
                            }
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // پاسخ ممکن است متن ساده یا HTML باشد.
            }
            catch (Exception ex)
            {
                LogException("ERROR MESSAGE PARSING", ex);
            }

            return defaultMessage;
        }

        private static bool TryGetPropertyIgnoreCase(
            JsonElement element,
            string propertyName,
            out JsonElement value)
        {
            value = default;

            if (element.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (string.Equals(
                    property.Name,
                    propertyName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }

            return false;
        }

        // =========================================================
        // HTTP ERROR MESSAGES
        // =========================================================

        private static string GetEmptyResponseMessage(
            HttpStatusCode statusCode,
            string operation)
        {
            return statusCode switch
            {
                HttpStatusCode.NoContent =>
                    $"سرور برای عملیات «{operation}» پاسخ خالی ارسال کرد.",

                HttpStatusCode.Unauthorized =>
                    $"برای عملیات «{operation}» احراز هویت انجام نشد.",

                HttpStatusCode.Forbidden =>
                    $"دسترسی برای عملیات «{operation}» مجاز نیست.",

                HttpStatusCode.NotFound =>
                    $"مسیر API مربوط به «{operation}» پیدا نشد.",

                HttpStatusCode.InternalServerError =>
                    $"سرور هنگام «{operation}» خطای داخلی ایجاد کرد.",

                HttpStatusCode.ServiceUnavailable =>
                    "سرویس موقتاً در دسترس نیست.",

                HttpStatusCode.GatewayTimeout =>
                    "زمان پاسخ سرویس به پایان رسید.",

                _ =>
                    $"سرور هنگام «{operation}» پاسخ خالی ارسال کرد."
            };
        }

        private static string GetDefaultHttpErrorMessage(
            HttpStatusCode statusCode)
        {
            return statusCode switch
            {
                HttpStatusCode.BadRequest =>
                    "درخواست ارسال‌شده نامعتبر است.",

                HttpStatusCode.Unauthorized =>
                    "احراز هویت انجام نشده یا نشست شما منقضی شده است.",

                HttpStatusCode.Forbidden =>
                    "دسترسی به این بخش مجاز نیست.",

                HttpStatusCode.NotFound =>
                    "مسیر یا اطلاعات موردنظر پیدا نشد.",

                HttpStatusCode.Conflict =>
                    "این اطلاعات قبلاً ثبت شده یا با وضعیت فعلی سازگار نیست.",

                HttpStatusCode.UnprocessableEntity =>
                    "اطلاعات ارسال‌شده قابل پردازش نیست.",

                HttpStatusCode.TooManyRequests =>
                    "تعداد درخواست‌ها بیش از حد مجاز است.",

                HttpStatusCode.InternalServerError =>
                    "خطای داخلی در API رخ داده است.",

                HttpStatusCode.BadGateway =>
                    "ارتباط با سرویس داخلی برقرار نشد.",

                HttpStatusCode.ServiceUnavailable =>
                    "سرویس موقتاً در دسترس نیست.",

                HttpStatusCode.GatewayTimeout =>
                    "زمان پاسخ سرویس به پایان رسید.",

                _ => "درخواست با خطا مواجه شد."
            };
        }

        // =========================================================
        // COMMON RESPONSES
        // =========================================================

        private static ApiResponse Failure(string message)
        {
            return new ApiResponse
            {
                Success = false,
                Message = message
            };
        }

        private static ApiResponse Success(string message)
        {
            return new ApiResponse
            {
                Success = true,
                Message = message
            };
        }

        // =========================================================
        // LOGGING
        // =========================================================

        private static void LogResponse(
            string operation,
            HttpResponseMessage response,
            string content)
        {
            Console.WriteLine("================================================");
            Console.WriteLine($"[{operation}] API RESPONSE");
            Console.WriteLine($"Status Code: {(int)response.StatusCode}");
            Console.WriteLine($"Status: {response.StatusCode}");
            Console.WriteLine(
                $"Content-Type: {response.Content.Headers.ContentType}");
            Console.WriteLine($"Content Length: {content.Length}");
            Console.WriteLine("Response body: [NOT LOGGED]");
            Console.WriteLine("================================================");
        }

        private static void LogException(
            string title,
            Exception exception)
        {
            Console.WriteLine("================================================");
            Console.WriteLine($"[{title}]");
            Console.WriteLine($"Exception Type: {exception.GetType().Name}");
            Console.WriteLine($"Message: {exception.Message}");
            Console.WriteLine("================================================");
        }
    }
}