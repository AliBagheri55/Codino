using System.Security.Cryptography;
using System.Text;
using Codino.Models.Api;
using Codino.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
namespace Codino.Controllers
{
    public class AccountController : Controller
    {
        // =========================================================
        // DEPENDENCIES
        // =========================================================

        private readonly ApiService _apiService;
        private readonly RedisRateLimitService _rateLimit;
        private readonly ILogger<AccountController> _logger;
        private readonly IConnectionMultiplexer _redis;


        // =========================================================
        // CONSTANTS
        // =========================================================

        private const int MaxFullNameLength = 100;
        private const int MaxPhoneLength = 11;
        private const int MaxPasswordLength = 128;

        private const int RegisterRateLimit = 3;
        private const int VerifyRegisterRateLimit = 5;
        private const int LoginRateLimit = 5;
        private const int ForgotPasswordRateLimit = 5;
        private const int ResetPasswordRateLimit = 5;

        // =========================================================
        // PROGRESSIVE LOGIN LOCK
        // =========================================================

        // اولین خطای رمز فقط هشدار است.

        // دومین خطای رمز:
        private const int SecondFailureLockMinutes = 2;

        // خطاهای سوم و چهارم:
        private const int MultipleFailureLockMinutes = 10;

        // خطای پنجم به بعد:
        private const int HeavyFailureLockMinutes = 30;

        // شمارنده خطاها تا 30 دقیقه نگهداری می‌شود.
        private const int LoginFailureWindowMinutes = 30;


        // =========================================================
        // SESSION KEYS
        // =========================================================

        private const string RegisterCaptchaKey =
            "RegisterCaptcha";

        private const string LoginCaptchaKey =
            "LoginCaptcha";

        private const string RegisterPhoneKey =
            "RegisterPhone";

        private const string RegisterFullNameKey =
            "RegisterFullName";

        private const string RegisterSecurityCode1Key =
            "RegisterSecurityCode1";

        private const string RegisterSecurityCode2Key =
            "RegisterSecurityCode2";

        private const string ResetPhoneKey =
            "ResetPasswordPhone";

        private const string ResetTokenKey =
            "ResetPasswordToken";

        private const string UserIdKey =
            "UserId";

        private const string UserFullNameKey =
            "UserFullName";

        private const string UserPhoneKey =
            "UserPhone";

        private const string UserPhoneNumberKey =
            "UserPhoneNumber";

        private const string AccessTokenKey =
            "AccessToken";

        private const string RefreshTokenKey =
            "RefreshToken";


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public AccountController(
            ApiService apiService,
            RedisRateLimitService rateLimit,
            ILogger<AccountController> logger,
            IConnectionMultiplexer redis)
        {
            _apiService =
                apiService
                ?? throw new ArgumentNullException(
                    nameof(apiService));

            _rateLimit =
                rateLimit
                ?? throw new ArgumentNullException(
                    nameof(rateLimit));

            _logger =
                logger
                ?? throw new ArgumentNullException(
                    nameof(logger));

            _redis =
                redis
                ?? throw new ArgumentNullException(
                    nameof(redis));
        }


        // =========================================================
        // REGISTER - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Register()
        {
            try
            {
                RemoveRegisterSensitiveSessionData();

                GenerateCaptcha("Register");

                await HttpContext.Session.CommitAsync();

                return View("Register");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while opening Register page.");

                ViewBag.Error =
                    "خطایی هنگام باز کردن صفحه ثبت‌نام رخ داد.";

                return View("Register");
            }
        }


        // =========================================================
        // REGISTER - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(64 * 1024)]
        public async Task<IActionResult> Register(
            string? fullName,
            string? phoneNumber,
            string? password,
            string? repeatPassword,
            string? captcha)
        {
            try
            {
                // =====================================================
                // NORMALIZE INPUT
                // =====================================================

                fullName =
                    NormalizeText(fullName);

                phoneNumber =
                    NormalizePhone(phoneNumber);

                password ??=
                    string.Empty;

                repeatPassword ??=
                    string.Empty;

                captcha =
                    NormalizeCaptcha(captcha);


                // =====================================================
                // CAPTCHA
                // =====================================================

                string sessionCaptcha =
                    NormalizeCaptcha(
                        HttpContext.Session.GetString(
                            RegisterCaptchaKey));

                if (string.IsNullOrWhiteSpace(
                    sessionCaptcha))
                {
                    return await RegisterError(
                        "جلسه کپچا منقضی شده است. لطفاً کپچای جدید دریافت کنید.");
                }

                if (string.IsNullOrWhiteSpace(captcha))
                {
                    return await RegisterError(
                        "لطفاً کد کپچا را وارد کنید.");
                }

                if (!CryptographicEquals(
                    sessionCaptcha,
                    captcha))
                {
                    RemoveRegisterCaptcha();

                    return await RegisterError(
                        "کد کپچا صحیح نیست.");
                }

                // کپچا فقط یک بار قابل استفاده است.
                RemoveRegisterCaptcha();


                // =====================================================
                // FULL NAME
                // =====================================================

                if (string.IsNullOrWhiteSpace(fullName))
                {
                    return await RegisterError(
                        "لطفاً نام و نام خانوادگی را وارد کنید.");
                }

                if (fullName.Length >
                    MaxFullNameLength)
                {
                    return await RegisterError(
                        "نام و نام خانوادگی بیش از حد مجاز طولانی است.");
                }


                // =====================================================
                // PHONE
                // =====================================================

                if (!IsValidPhoneNumber(phoneNumber))
                {
                    return await RegisterError(
                        "شماره موبایل معتبر نیست.");
                }


                // =====================================================
                // PASSWORD
                // =====================================================

                if (!IsValidPassword(password))
                {
                    return await RegisterError(
                        "رمز عبور باید حداقل ۸ کاراکتر داشته باشد و ترکیبی از حروف و اعداد باشد.");
                }

                if (password != repeatPassword)
                {
                    return await RegisterError(
                        "رمز عبور و تکرار رمز عبور یکسان نیستند.");
                }


                // =====================================================
                // RATE LIMIT
                // =====================================================

                string ip =
                    GetClientIp();

                string rateKey =
                    $"register-rate:{ip}:{HashForRateLimit(phoneNumber)}";

                bool allowed =
                    await _rateLimit.IsAllowedAsync(
                        rateKey,
                        RegisterRateLimit,
                        TimeSpan.FromMinutes(10));

                if (!allowed)
                {
                    return await RegisterError(
                        "تعداد درخواست‌های ثبت‌نام بیش از حد مجاز است. لطفاً بعداً دوباره تلاش کنید.");
                }


                // =====================================================
                // API REQUEST
                // =====================================================

                var request =
                    new RegisterRequest
                    {
                        FullName =
                            fullName,

                        PhoneNumber =
                            phoneNumber,

                        Password =
                            password,

                        RepeatPassword =
                            repeatPassword
                    };

                ApiResponse? response =
                    await _apiService.RegisterAsync(
                        request);

                if (response == null)
                {
                    return await RegisterError(
                        "ارتباط با سرور برقرار نشد.");
                }

                if (!response.Success)
                {
                    return await RegisterError(
                        GetSafeApiMessage(
                            response.Message,
                            "ثبت‌نام انجام نشد."));
                }


                // =====================================================
                // SAVE REGISTRATION DATA
                // =====================================================

                HttpContext.Session.SetString(
                    RegisterPhoneKey,
                    phoneNumber);

                HttpContext.Session.SetString(
                    RegisterFullNameKey,
                    fullName);

                if (!string.IsNullOrWhiteSpace(
                    response.SecurityCode1))
                {
                    HttpContext.Session.SetString(
                        RegisterSecurityCode1Key,
                        response.SecurityCode1);
                }

                if (!string.IsNullOrWhiteSpace(
                    response.SecurityCode2))
                {
                    HttpContext.Session.SetString(
                        RegisterSecurityCode2Key,
                        response.SecurityCode2);
                }

                await HttpContext.Session.CommitAsync();

                return RedirectToAction(
                    nameof(VerifyRegisterPage));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "HTTP error during registration.");

                return await RegisterError(
                    "ارتباط با سرور برقرار نشد.");
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(
                    ex,
                    "Register API request timed out.");

                return await RegisterError(
                    "زمان پاسخ‌گویی سرور به پایان رسید.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error during registration.");

                return await RegisterError(
                    "خطایی هنگام ثبت‌نام رخ داد.");
            }
        }


        // =========================================================
        // REGISTER ERROR
        // =========================================================

        private async Task<IActionResult> RegisterError(
            string error)
        {
            ViewBag.Error =
                GetSafeErrorMessage(error);

            RemoveRegisterCaptcha();

            GenerateCaptcha("Register");

            await HttpContext.Session.CommitAsync();

            return View("Register");
        }


        // =========================================================
        // VERIFY REGISTER - GET
        // =========================================================

        [HttpGet]
        public IActionResult VerifyRegisterPage()
        {
            string? phoneNumber =
                HttpContext.Session.GetString(
                    RegisterPhoneKey);

            if (!IsValidPhoneNumber(phoneNumber))
            {
                return RedirectToAction(
                    nameof(Register));
            }

            ViewBag.PhoneNumber =
                phoneNumber;

            ViewBag.SecurityCode1 =
                HttpContext.Session.GetString(
                    RegisterSecurityCode1Key);

            ViewBag.SecurityCode2 =
                HttpContext.Session.GetString(
                    RegisterSecurityCode2Key);

            return View("VerifyRegister");
        }


        // =========================================================
        // VERIFY REGISTER - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(32 * 1024)]
        public async Task<IActionResult> VerifyRegister()
        {
            string phoneNumber =
                HttpContext.Session.GetString(
                    RegisterPhoneKey)
                ?? string.Empty;

            try
            {
                if (!IsValidPhoneNumber(phoneNumber))
                {
                    ClearRegistrationSession();

                    return RedirectToAction(
                        nameof(Register));
                }

                if (!Request.HasFormContentType)
                {
                    return ShowVerifyRegisterError(
                        phoneNumber,
                        "اطلاعات فرم دریافت نشد.");
                }

                var form =
                    await Request.ReadFormAsync();

                string code =
                    NormalizeDigits(
                        form["code"].ToString());

                if (string.IsNullOrWhiteSpace(code))
                {
                    return ShowVerifyRegisterError(
                        phoneNumber,
                        "کد امنیتی را وارد کنید.");
                }

                if (code.Length != 4 ||
                    !code.All(char.IsDigit))
                {
                    return ShowVerifyRegisterError(
                        phoneNumber,
                        "کد امنیتی باید ۴ رقم باشد.");
                }


                // =====================================================
                // RATE LIMIT
                // =====================================================

                string ip =
                    GetClientIp();

                string rateKey =
                    $"verify-register-rate:{ip}:{HashForRateLimit(phoneNumber)}";

                bool allowed =
                    await _rateLimit.IsAllowedAsync(
                        rateKey,
                        VerifyRegisterRateLimit,
                        TimeSpan.FromMinutes(10));

                if (!allowed)
                {
                    return ShowVerifyRegisterError(
                        phoneNumber,
                        "تعداد تلاش‌های تأیید بیش از حد مجاز است. لطفاً بعداً دوباره تلاش کنید.");
                }


                // =====================================================
                // API
                // =====================================================

                ApiResponse? response =
                    await _apiService.VerifyRegisterAsync(
                        phoneNumber,
                        code);

                if (response == null)
                {
                    return ShowVerifyRegisterError(
                        phoneNumber,
                        "ارتباط با سرور برقرار نشد.");
                }

                if (!response.Success)
                {
                    return ShowVerifyRegisterError(
                        phoneNumber,
                        GetSafeApiMessage(
                            response.Message,
                            "تأیید ثبت‌نام انجام نشد."));
                }


                // =====================================================
                // SUCCESS
                // =====================================================

                ClearRegistrationSession();

                await HttpContext.Session.CommitAsync();

                TempData["AccountSuccess"] =
                    "ثبت‌نام با موفقیت انجام شد. اکنون وارد حساب خود شوید.";

                return RedirectToAction(
                    nameof(Login));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "HTTP error during registration verification.");

                return ShowVerifyRegisterError(
                    phoneNumber,
                    "ارتباط با سرور برقرار نشد.");
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(
                    ex,
                    "Verify register API request timed out.");

                return ShowVerifyRegisterError(
                    phoneNumber,
                    "زمان پاسخ‌گویی سرور به پایان رسید.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error during registration verification.");

                return ShowVerifyRegisterError(
                    phoneNumber,
                    "خطایی هنگام تأیید ثبت‌نام رخ داد.");
            }
        }


        // =========================================================
        // VERIFY ERROR
        // =========================================================

        private IActionResult ShowVerifyRegisterError(
            string phoneNumber,
            string error)
        {
            ViewBag.Error =
                GetSafeErrorMessage(error);

            ViewBag.PhoneNumber =
                phoneNumber;

            ViewBag.SecurityCode1 =
                HttpContext.Session.GetString(
                    RegisterSecurityCode1Key);

            ViewBag.SecurityCode2 =
                HttpContext.Session.GetString(
                    RegisterSecurityCode2Key);

            return View("VerifyRegister");
        }


        // =========================================================
        // LOGIN - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Login()
        {
            try
            {
                RemoveLoginCaptcha();

                GenerateCaptcha("Login");

                await HttpContext.Session.CommitAsync();

                return View("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while opening Login page.");

                ViewBag.Error =
                    "خطایی هنگام باز کردن صفحه ورود رخ داد.";

                return View("Login");
            }
        }


        // =========================================================
        // LOGIN - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(64 * 1024)]
        public async Task<IActionResult> Login(
            string? phoneNumber,
            string? password,
            string? captcha)
        {
            string normalizedPhone = string.Empty;

            try
            {
                // =====================================================
                // NORMALIZE INPUT
                // =====================================================

                normalizedPhone =
                    NormalizePhone(phoneNumber);

                password ??=
                    string.Empty;

                captcha =
                    NormalizeCaptcha(captcha);


                // =====================================================
                // CAPTCHA
                // =====================================================

                string sessionCaptcha =
                    NormalizeCaptcha(
                        HttpContext.Session.GetString(
                            LoginCaptchaKey));

                if (string.IsNullOrWhiteSpace(
                    sessionCaptcha))
                {
                    return await LoginError(
                        "جلسه کپچا منقضی شده است. لطفاً کپچای جدید دریافت کنید.");
                }

                if (string.IsNullOrWhiteSpace(captcha))
                {
                    return await LoginError(
                        "لطفاً کد کپچا را وارد کنید.");
                }

                if (!CryptographicEquals(
                    sessionCaptcha,
                    captcha))
                {
                    RemoveLoginCaptcha();

                    return await LoginError(
                        "کد کپچا صحیح نیست.");
                }

                // کپچا فقط یک بار قابل استفاده است.
                RemoveLoginCaptcha();


                // =====================================================
                // PHONE
                // =====================================================

                if (!IsValidPhoneNumber(normalizedPhone))
                {
                    return await LoginError(
                        "شماره موبایل یا رمز عبور صحیح نیست.");
                }


                // =====================================================
                // PASSWORD
                // =====================================================

                if (string.IsNullOrWhiteSpace(password) ||
                    password.Length > MaxPasswordLength)
                {
                    return await LoginError(
                        "شماره موبایل یا رمز عبور صحیح نیست.");
                }


                // =====================================================
                // REDIS KEYS
                // =====================================================

                string ip =
                    GetClientIp();

                string phoneHash =
                    HashForRateLimit(
                        normalizedPhone);

                string loginLockKey =
                    $"login-lock:{ip}:{phoneHash}";

                string loginFailureKey =
                    $"login-failures:{ip}:{phoneHash}";


                // =====================================================
                // CHECK PROGRESSIVE LOCK
                // =====================================================

                IDatabase redis =
                    _redis.GetDatabase();

                RedisValue currentLock =
                    await redis.StringGetAsync(
                        loginLockKey);

                if (currentLock.HasValue)
                {
                    TimeSpan? remaining =
                        await redis.KeyTimeToLiveAsync(
                            loginLockKey);

                    int remainingMinutes = 1;

                    if (remaining.HasValue &&
                        remaining.Value > TimeSpan.Zero)
                    {
                        remainingMinutes =
                            Math.Max(
                                1,
                                (int)Math.Ceiling(
                                    remaining.Value.TotalMinutes));
                    }

                    return await LoginError(
                        $"ورود شما موقتاً مسدود است. لطفاً حدود {remainingMinutes} دقیقه دیگر دوباره تلاش کنید.");
                }


                // =====================================================
                // RATE LIMIT
                // =====================================================

                string rateKey =
                    $"login-rate:{ip}:{phoneHash}";

                bool allowed =
                    await _rateLimit.IsAllowedAsync(
                        rateKey,
                        LoginRateLimit,
                        TimeSpan.FromMinutes(1));

                if (!allowed)
                {
                    return await LoginError(
                        "تعداد تلاش‌های ورود بیش از حد مجاز است. لطفاً کمی بعد دوباره تلاش کنید.");
                }


                // =====================================================
                // API REQUEST
                // =====================================================

                var request =
                    new LoginRequest
                    {
                        PhoneNumber =
                            normalizedPhone,

                        Password =
                            password
                    };

                LoginResponse? response =
                    await _apiService.LoginAsync(
                        request);

                // =====================================================
                // API UNAVAILABLE
                // =====================================================

                if (response == null)
                {
                    // در خطای سرور شمارنده خطا را زیاد نمی‌کنیم.
                    return await LoginError(
                        "ارتباط با سرور برقرار نشد.");
                }


                // =====================================================
                // WRONG LOGIN
                // =====================================================

                if (!response.Success)
                {
                    // افزایش اتمیک شمارنده خطا در Redis.
                    long failureCount =
                        await redis.StringIncrementAsync(
                            loginFailureKey);


                    // اولین خطا:
                    // TTL برای کل پنجره خطاها تنظیم می‌شود.
                    if (failureCount == 1)
                    {
                        await redis.KeyExpireAsync(
                            loginFailureKey,
                            TimeSpan.FromMinutes(
                                LoginFailureWindowMinutes));
                    }


                    // =================================================
                    // FIRST FAILURE
                    // =================================================

                    if (failureCount == 1)
                    {
                        return await LoginError(
                            "شماره موبایل یا رمز عبور صحیح نیست.  توجه: در صورت تکرار رمز اشتباه، ورود شما موقتاً مسدود خواهد شد.");
                    }


                    // =================================================
                    // SECOND FAILURE
                    // =================================================

                    if (failureCount == 2)
                    {
                        await redis.StringSetAsync(
                            loginLockKey,
                            "2-minute-lock",
                            TimeSpan.FromMinutes(
                                SecondFailureLockMinutes));

                        return await LoginError(
                            "شماره موبایل یا رمز عبور صحیح نیست.  به دلیل دو تلاش ناموفق، ورود شما به مدت ۲ دقیقه مسدود شد.");
                    }


                    // =================================================
                    // THIRD + FOURTH FAILURE
                    // =================================================

                    if (failureCount <= 4)
                    {
                        await redis.StringSetAsync(
                            loginLockKey,
                            "10-minute-lock",
                            TimeSpan.FromMinutes(
                                MultipleFailureLockMinutes));

                        return await LoginError(
                            "تلاش‌های ناموفق متعددی برای ورود انجام شده است.  ورود شما به مدت ۱۰ دقیقه مسدود شد.");
                    }


                    // =================================================
                    // FIFTH FAILURE AND MORE
                    // =================================================

                    await redis.StringSetAsync(
                        loginLockKey,
                        "30-minute-lock",
                        TimeSpan.FromMinutes(
                            HeavyFailureLockMinutes));

                    return await LoginError(
                        "تعداد تلاش‌های ناموفق ورود بیش از حد مجاز است.  ورود شما به مدت ۳۰ دقیقه مسدود شد.");
                }


                // =====================================================
                // VALIDATE LOGIN RESPONSE
                // =====================================================

                if (response.UserId == null ||
                    response.UserId.Value <= 0)
                {
                    _logger.LogError(
                        "Login succeeded but UserId is invalid.");

                    return await LoginError(
                        "اطلاعات حساب کاربری ناقص دریافت شد.");
                }

                if (string.IsNullOrWhiteSpace(
                    response.AccessToken))
                {
                    _logger.LogError(
                        "Login succeeded but AccessToken is empty.");

                    return await LoginError(
                        "توکن ورود دریافت نشد.");
                }


                // =====================================================
                // SUCCESSFUL LOGIN
                // =====================================================
                //
                // ورود موفق:
                //
                // 1. شمارنده خطا پاک می‌شود.
                // 2. قفل احتمالی پاک می‌شود.
                //
                // =====================================================

                await redis.KeyDeleteAsync(
                    loginFailureKey);

                await redis.KeyDeleteAsync(
                    loginLockKey);


                // =====================================================
                // REMOVE OLD AUTH DATA
                // =====================================================

                ClearAuthenticationSession();


                // =====================================================
                // CREATE NEW AUTH DATA
                // =====================================================

                HttpContext.Session.SetString(
                    UserIdKey,
                    response.UserId.Value.ToString());

                HttpContext.Session.SetString(
                    UserFullNameKey,
                    NormalizeText(
                        response.FullName));

                HttpContext.Session.SetString(
                    UserPhoneKey,
                    normalizedPhone);

                HttpContext.Session.SetString(
                    UserPhoneNumberKey,
                    normalizedPhone);

                HttpContext.Session.SetString(
                    AccessTokenKey,
                    response.AccessToken);


                if (!string.IsNullOrWhiteSpace(
                    response.RefreshToken))
                {
                    HttpContext.Session.SetString(
                        RefreshTokenKey,
                        response.RefreshToken);
                }


                // =====================================================
                // SAVE SESSION
                // =====================================================

                await HttpContext.Session.CommitAsync();


                _logger.LogInformation(
                    "User {UserId} logged in successfully.",
                    response.UserId.Value);


                return RedirectToAction(
                    "Index",
                    "Home");
            }
            catch (RedisException ex)
            {
                _logger.LogError(
                    ex,
                    "Redis error during login.");

                return await LoginError(
                    "سرویس امنیتی ورود موقتاً در دسترس نیست. لطفاً دوباره تلاش کنید.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "HTTP error during login.");

                return await LoginError(
                    "ارتباط با سرور برقرار نشد.");
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(
                    ex,
                    "Login API request timed out.");

                return await LoginError(
                    "زمان پاسخ‌گویی سرور به پایان رسید.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error during login.");

                return await LoginError(
                    "خطایی هنگام ورود به حساب رخ داد.");
            }
        }


        // =========================================================
        // LOGIN ERROR
        // =========================================================

        private async Task<IActionResult> LoginError(
            string error)
        {
            ViewBag.Error =
                GetSafeErrorMessage(error);

            RemoveLoginCaptcha();

            GenerateCaptcha("Login");

            await HttpContext.Session.CommitAsync();

            return View("Login");
        }


        // =========================================================
        // LOGOUT
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            try
            {
                ClearAuthenticationSession();

                ClearRegistrationSession();

                ClearResetPasswordSession();

                RemoveLoginCaptcha();

                RemoveRegisterCaptcha();

                HttpContext.Session.Clear();

                await HttpContext.Session.CommitAsync();

                Response.Cookies.Delete(
                    ".Codino.Session");

                _logger.LogInformation(
                    "User logged out successfully.");

                return RedirectToAction(
                    "Index",
                    "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error during logout.");

                try
                {
                    HttpContext.Session.Clear();

                    Response.Cookies.Delete(
                        ".Codino.Session");
                }
                catch
                {
                    // عمداً خالی است.
                }

                return RedirectToAction(
                    "Index",
                    "Home");
            }
        }


        // =========================================================
        // FORGOT PASSWORD - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> ForgotPassword()
        {
            try
            {
                ClearResetPasswordSession();

                CaptchaResponse? captcha =
                    await _apiService.GetCaptchaAsync();

                if (captcha == null ||
                    !captcha.Success)
                {
                    ViewBag.Error =
                        "دریافت کپچا امکان‌پذیر نیست.";

                    return View("ForgotPassword");
                }

                ViewBag.CaptchaId =
                    captcha.CaptchaId;

                ViewBag.CaptchaImage =
                    captcha.Image;

                return View("ForgotPassword");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while opening ForgotPassword page.");

                ViewBag.Error =
                    "دریافت کپچا امکان‌پذیر نیست.";

                return View("ForgotPassword");
            }
        }


        // =========================================================
        // FORGOT PASSWORD - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(64 * 1024)]
        public async Task<IActionResult> ForgotPassword(
            string? phoneNumber,
            string? securityCode,
            string? captchaId,
            string? captcha)
        {
            try
            {
                phoneNumber =
                    NormalizePhone(phoneNumber);

                securityCode =
                    NormalizeDigits(securityCode);

                captchaId =
                    NormalizeText(captchaId);

                captcha =
                    NormalizeCaptcha(captcha);


                // =====================================================
                // PHONE
                // =====================================================

                if (!IsValidPhoneNumber(phoneNumber))
                {
                    return await ShowForgotPasswordError(
                        "اطلاعات وارد شده صحیح نیست.");
                }


                // =====================================================
                // SECURITY CODE
                // =====================================================

                if (securityCode.Length != 4 ||
                    !securityCode.All(char.IsDigit))
                {
                    return await ShowForgotPasswordError(
                        "اطلاعات وارد شده صحیح نیست.");
                }


                // =====================================================
                // CAPTCHA
                // =====================================================

                if (string.IsNullOrWhiteSpace(captchaId) ||
                    string.IsNullOrWhiteSpace(captcha))
                {
                    return await ShowForgotPasswordError(
                        "اطلاعات کپچا صحیح نیست.");
                }


                // =====================================================
                // RATE LIMIT
                // =====================================================

                string ip =
                    GetClientIp();

                string rateKey =
                    $"forgot-password-rate:{ip}:{HashForRateLimit(phoneNumber)}";

                bool allowed =
                    await _rateLimit.IsAllowedAsync(
                        rateKey,
                        ForgotPasswordRateLimit,
                        TimeSpan.FromMinutes(10));

                if (!allowed)
                {
                    return await ShowForgotPasswordError(
                        "تعداد تلاش‌های بازیابی رمز بیش از حد مجاز است. لطفاً بعداً دوباره تلاش کنید.");
                }


                // =====================================================
                // API REQUEST
                // =====================================================

                var request =
                    new ForgotPasswordRequest
                    {
                        PhoneNumber =
                            phoneNumber,

                        SecurityCode =
                            securityCode,

                        CaptchaId =
                            captchaId,

                        Captcha =
                            captcha
                    };

                ForgotPasswordResponse? response =
                    await _apiService.ForgotPasswordAsync(
                        request);

                if (response == null)
                {
                    return await ShowForgotPasswordError(
                        "ارتباط با سرور برقرار نشد.");
                }

                if (!response.Success)
                {
                    return await ShowForgotPasswordError(
                        "اطلاعات وارد شده صحیح نیست.");
                }

                if (string.IsNullOrWhiteSpace(
                    response.ResetToken))
                {
                    _logger.LogError(
                        "Forgot password succeeded but ResetToken is empty.");

                    return await ShowForgotPasswordError(
                        "امکان ادامه بازیابی رمز وجود ندارد.");
                }


                // =====================================================
                // SAVE RESET INFORMATION
                // =====================================================

                HttpContext.Session.SetString(
                    ResetPhoneKey,
                    phoneNumber);

                HttpContext.Session.SetString(
                    ResetTokenKey,
                    response.ResetToken);

                await HttpContext.Session.CommitAsync();

                return RedirectToAction(
                    nameof(ResetPassword));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "HTTP error during forgot password.");

                return await ShowForgotPasswordError(
                    "ارتباط با سرور برقرار نشد.");
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(
                    ex,
                    "Forgot password API request timed out.");

                return await ShowForgotPasswordError(
                    "زمان پاسخ‌گویی سرور به پایان رسید.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error during forgot password.");

                return await ShowForgotPasswordError(
                    "خطایی هنگام بازیابی رمز عبور رخ داد.");
            }
        }


        // =========================================================
        // FORGOT PASSWORD ERROR
        // =========================================================

        private async Task<IActionResult>
            ShowForgotPasswordError(
                string error)
        {
            ViewBag.Error =
                GetSafeErrorMessage(error);

            try
            {
                CaptchaResponse? captcha =
                    await _apiService.GetCaptchaAsync();

                if (captcha != null &&
                    captcha.Success)
                {
                    ViewBag.CaptchaId =
                        captcha.CaptchaId;

                    ViewBag.CaptchaImage =
                        captcha.Image;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while generating forgot password CAPTCHA.");
            }

            return View("ForgotPassword");
        }


        // =========================================================
        // RESET PASSWORD - GET
        // =========================================================

        [HttpGet]
        public IActionResult ResetPassword()
        {
            string? phoneNumber =
                HttpContext.Session.GetString(
                    ResetPhoneKey);

            string? resetToken =
                HttpContext.Session.GetString(
                    ResetTokenKey);

            if (!IsValidPhoneNumber(phoneNumber) ||
                string.IsNullOrWhiteSpace(resetToken))
            {
                ClearResetPasswordSession();

                return RedirectToAction(
                    nameof(ForgotPassword));
            }

            ViewBag.PhoneNumber =
                phoneNumber;

            return View("ResetPassword");
        }


        // =========================================================
        // RESET PASSWORD - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(64 * 1024)]
        public async Task<IActionResult> ResetPassword(
            string? newPassword,
            string? repeatPassword)
        {
            string? phoneNumber =
                HttpContext.Session.GetString(
                    ResetPhoneKey);

            string? resetToken =
                HttpContext.Session.GetString(
                    ResetTokenKey);

            try
            {
                newPassword ??=
                    string.Empty;

                repeatPassword ??=
                    string.Empty;


                // =====================================================
                // SESSION VALIDATION
                // =====================================================

                if (!IsValidPhoneNumber(phoneNumber) ||
                    string.IsNullOrWhiteSpace(resetToken))
                {
                    ClearResetPasswordSession();

                    return RedirectToAction(
                        nameof(ForgotPassword));
                }


                // =====================================================
                // PASSWORD VALIDATION
                // =====================================================

                if (!IsValidPassword(newPassword))
                {
                    return ShowResetPasswordError(
                        phoneNumber,
                        "رمز عبور باید حداقل ۸ کاراکتر داشته باشد و ترکیبی از حروف و اعداد باشد.");
                }

                if (newPassword != repeatPassword)
                {
                    return ShowResetPasswordError(
                        phoneNumber,
                        "رمزهای عبور یکسان نیستند.");
                }


                // =====================================================
                // RATE LIMIT
                // =====================================================

                string ip =
                    GetClientIp();

                string rateKey =
                    $"reset-password-rate:{ip}:{HashForRateLimit(phoneNumber)}";

                bool allowed =
                    await _rateLimit.IsAllowedAsync(
                        rateKey,
                        ResetPasswordRateLimit,
                        TimeSpan.FromMinutes(10));

                if (!allowed)
                {
                    return ShowResetPasswordError(
                        phoneNumber,
                        "تعداد تلاش‌های تغییر رمز بیش از حد مجاز است. لطفاً بعداً دوباره تلاش کنید.");
                }


                // =====================================================
                // API REQUEST
                // =====================================================

                var request =
                    new ResetPasswordRequest
                    {
                        PhoneNumber =
                            phoneNumber,

                        ResetToken =
                            resetToken,

                        NewPassword =
                            newPassword,

                        RepeatPassword =
                            repeatPassword
                    };

                ApiResponse? response =
                    await _apiService.ResetPasswordAsync(
                        request);

                if (response == null)
                {
                    return ShowResetPasswordError(
                        phoneNumber,
                        "ارتباط با سرور برقرار نشد.");
                }

                if (!response.Success)
                {
                    return ShowResetPasswordError(
                        phoneNumber,
                        GetSafeApiMessage(
                            response.Message,
                            "تغییر رمز عبور انجام نشد."));
                }


                // =====================================================
                // RESET TOKEN DESTROYED
                // =====================================================

                ClearResetPasswordSession();

                await HttpContext.Session.CommitAsync();

                TempData["AccountSuccess"] =
                    "رمز عبور با موفقیت تغییر کرد. اکنون وارد حساب خود شوید.";

                return RedirectToAction(
                    nameof(Login));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "HTTP error during password reset.");

                return ShowResetPasswordError(
                    phoneNumber,
                    "ارتباط با سرور برقرار نشد.");
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(
                    ex,
                    "Password reset API request timed out.");

                return ShowResetPasswordError(
                    phoneNumber,
                    "زمان پاسخ‌گویی سرور به پایان رسید.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error during password reset.");

                return ShowResetPasswordError(
                    phoneNumber,
                    "خطایی هنگام تغییر رمز عبور رخ داد.");
            }
        }


        // =========================================================
        // RESET PASSWORD ERROR
        // =========================================================

        private IActionResult ShowResetPasswordError(
            string? phoneNumber,
            string error)
        {
            ViewBag.PhoneNumber =
                phoneNumber;

            ViewBag.Error =
                GetSafeErrorMessage(error);

            return View("ResetPassword");
        }


        // =========================================================
        // CAPTCHA GENERATOR
        // =========================================================

        private void GenerateCaptcha(
            string type)
        {
            const string characters =
                "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

            Span<char> captchaChars =
                stackalloc char[5];

            for (int i = 0;
                 i < captchaChars.Length;
                 i++)
            {
                int index =
                    RandomNumberGenerator.GetInt32(
                        characters.Length);

                captchaChars[i] =
                    characters[index];
            }

            string captcha =
                new string(captchaChars);

            switch (type)
            {
                case "Login":

                    HttpContext.Session.SetString(
                        LoginCaptchaKey,
                        captcha);

                    break;

                case "Register":

                    HttpContext.Session.SetString(
                        RegisterCaptchaKey,
                        captcha);

                    break;
            }

            ViewBag.Captcha =
                captcha;
        }


        // =========================================================
        // REMOVE LOGIN CAPTCHA
        // =========================================================

        private void RemoveLoginCaptcha()
        {
            HttpContext.Session.Remove(
                LoginCaptchaKey);
        }


        // =========================================================
        // REMOVE REGISTER CAPTCHA
        // =========================================================

        private void RemoveRegisterCaptcha()
        {
            HttpContext.Session.Remove(
                RegisterCaptchaKey);
        }


        // =========================================================
        // REMOVE REGISTER SENSITIVE DATA
        // =========================================================

        private void RemoveRegisterSensitiveSessionData()
        {
            HttpContext.Session.Remove(
                RegisterCaptchaKey);

            HttpContext.Session.Remove(
                RegisterPhoneKey);

            HttpContext.Session.Remove(
                RegisterFullNameKey);

            HttpContext.Session.Remove(
                RegisterSecurityCode1Key);

            HttpContext.Session.Remove(
                RegisterSecurityCode2Key);
        }


        // =========================================================
        // CLEAR REGISTRATION SESSION
        // =========================================================

        private void ClearRegistrationSession()
        {
            HttpContext.Session.Remove(
                RegisterCaptchaKey);

            HttpContext.Session.Remove(
                RegisterPhoneKey);

            HttpContext.Session.Remove(
                RegisterFullNameKey);

            HttpContext.Session.Remove(
                RegisterSecurityCode1Key);

            HttpContext.Session.Remove(
                RegisterSecurityCode2Key);
        }


        // =========================================================
        // CLEAR RESET PASSWORD SESSION
        // =========================================================

        private void ClearResetPasswordSession()
        {
            HttpContext.Session.Remove(
                ResetPhoneKey);

            HttpContext.Session.Remove(
                ResetTokenKey);
        }


        // =========================================================
        // CLEAR AUTHENTICATION SESSION
        // =========================================================

        private void ClearAuthenticationSession()
        {
            HttpContext.Session.Remove(
                UserIdKey);

            HttpContext.Session.Remove(
                UserFullNameKey);

            HttpContext.Session.Remove(
                UserPhoneKey);

            HttpContext.Session.Remove(
                UserPhoneNumberKey);

            HttpContext.Session.Remove(
                AccessTokenKey);

            HttpContext.Session.Remove(
                RefreshTokenKey);
        }


        // =========================================================
        // NORMALIZE CAPTCHA
        // =========================================================

        private static string NormalizeCaptcha(
            string? captcha)
        {
            if (string.IsNullOrWhiteSpace(captcha))
            {
                return string.Empty;
            }

            return captcha
                .Trim()
                .Replace(
                    " ",
                    string.Empty)
                .ToUpperInvariant();
        }


        // =========================================================
        // NORMALIZE TEXT
        // =========================================================

        private static string NormalizeText(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Trim();
        }


        // =========================================================
        // NORMALIZE PHONE
        // =========================================================

        private static string NormalizePhone(
            string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return string.Empty;
            }

            phone =
                phone.Trim()
                    .Replace(" ", string.Empty)
                    .Replace("-", string.Empty)
                    .Replace("(", string.Empty)
                    .Replace(")", string.Empty);

            phone =
                NormalizeDigits(phone);


            // +989121234567
            // تبدیل به:
            // 09121234567

            if (phone.StartsWith(
                "+98",
                StringComparison.Ordinal))
            {
                phone =
                    "0" + phone[3..];
            }
            else if (phone.StartsWith(
                         "0098",
                         StringComparison.Ordinal))
            {
                phone =
                    "0" + phone[4..];
            }

            return phone;
        }


        // =========================================================
        // NORMALIZE DIGITS
        // =========================================================

        private static string NormalizeDigits(
            string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            char[] chars =
                value.ToCharArray();

            for (int i = 0;
                 i < chars.Length;
                 i++)
            {
                chars[i] =
                    chars[i] switch
                    {
                        '۰' => '0',
                        '۱' => '1',
                        '۲' => '2',
                        '۳' => '3',
                        '۴' => '4',
                        '۵' => '5',
                        '۶' => '6',
                        '۷' => '7',
                        '۸' => '8',
                        '۹' => '9',

                        '٠' => '0',
                        '١' => '1',
                        '٢' => '2',
                        '٣' => '3',
                        '٤' => '4',
                        '٥' => '5',
                        '٦' => '6',
                        '٧' => '7',
                        '٨' => '8',
                        '٩' => '9',

                        _ => chars[i]
                    };
            }

            return new string(chars);
        }


        // =========================================================
        // PHONE VALIDATION
        // =========================================================

        private static bool IsValidPhoneNumber(
            string? phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(
                phoneNumber))
            {
                return false;
            }

            if (phoneNumber.Length !=
                MaxPhoneLength)
            {
                return false;
            }

            if (!phoneNumber.StartsWith(
                "09",
                StringComparison.Ordinal))
            {
                return false;
            }

            return phoneNumber.All(
                char.IsDigit);
        }


        // =========================================================
        // PASSWORD VALIDATION
        // =========================================================

        private static bool IsValidPassword(
            string? password)
        {
            if (string.IsNullOrWhiteSpace(
                password))
            {
                return false;
            }

            if (password.Length < 8 ||
                password.Length > MaxPasswordLength)
            {
                return false;
            }

            bool hasLetter =
                password.Any(
                    char.IsLetter);

            bool hasDigit =
                password.Any(
                    char.IsDigit);

            return hasLetter &&
                   hasDigit;
        }


        // =========================================================
        // CONSTANT-TIME STRING COMPARISON
        // =========================================================

        private static bool CryptographicEquals(
            string? a,
            string? b)
        {
            if (a == null ||
                b == null)
            {
                return false;
            }

            byte[] aBytes =
                Encoding.UTF8.GetBytes(a);

            byte[] bBytes =
                Encoding.UTF8.GetBytes(b);

            if (aBytes.Length !=
                bBytes.Length)
            {
                return false;
            }

            return CryptographicOperations
                .FixedTimeEquals(
                    aBytes,
                    bBytes);
        }


        // =========================================================
        // CLIENT IP
        // =========================================================

        private string GetClientIp()
        {
            string? ip =
                HttpContext.Connection
                    .RemoteIpAddress?
                    .ToString();

            if (string.IsNullOrWhiteSpace(ip))
            {
                return "unknown";
            }

            return ip;
        }


        // =========================================================
        // HASH VALUE FOR RATE LIMIT
        // =========================================================

        private static string HashForRateLimit(
            string? value)
        {
            value ??= string.Empty;

            byte[] bytes =
                Encoding.UTF8.GetBytes(value);

            byte[] hash =
                SHA256.HashData(bytes);

            return Convert
                .ToHexString(hash)
                .ToLowerInvariant();
        }


        // =========================================================
        // SAFE API MESSAGE
        // =========================================================

        private static string GetSafeApiMessage(
            string? message,
            string fallback)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return fallback;
            }

            if (message.Length > 300)
            {
                return fallback;
            }

            return message.Trim();
        }


        // =========================================================
        // SAFE ERROR MESSAGE
        // =========================================================

        private static string GetSafeErrorMessage(
            string? error)
        {
            if (string.IsNullOrWhiteSpace(error))
            {
                return "خطایی رخ داد.";
            }

            if (error.Length > 300)
            {
                return "خطایی رخ داد.";
            }

            return error.Trim();
        }


        // =========================================================
        // USER PROFILE
        // =========================================================

        [HttpGet]
        public IActionResult Profile()
        {
            string? userId =
                HttpContext.Session.GetString(
                    UserIdKey);

            string? userFullName =
                HttpContext.Session.GetString(
                    UserFullNameKey);

            string? userPhone =
                HttpContext.Session.GetString(
                    UserPhoneKey);

            if (string.IsNullOrWhiteSpace(userPhone))
            {
                userPhone =
                    HttpContext.Session.GetString(
                        UserPhoneNumberKey);
            }


            // =====================================================
            // AUTH CHECK
            // =====================================================

            if (string.IsNullOrWhiteSpace(userId) ||
                !int.TryParse(
                    userId,
                    out int parsedUserId) ||
                parsedUserId <= 0 ||
                string.IsNullOrWhiteSpace(userFullName))
            {
                return RedirectToAction(
                    nameof(Login));
            }

            ViewBag.UserId =
                parsedUserId;

            ViewBag.UserFullName =
                userFullName;

            ViewBag.UserPhone =
                string.IsNullOrWhiteSpace(userPhone)
                    ? "شماره موبایل در نشست فعلی موجود نیست."
                    : userPhone;

            ViewBag.ProfileImage =
                Url.Content(
                    "~/images/codino-intro.jpg");

            return View("Profile");
        }
    }
}