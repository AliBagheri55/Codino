using Codino.Api.Data;
using Codino.Api.DTOs;
using Codino.Api.Models;
using Codino.Api.Services;
using Codino.Api.Settings;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

using StackExchange.Redis;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Codino.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [RequestSizeLimit(32 * 1024)]
    public class AuthController : ControllerBase
    {
        private readonly CodinoDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly IConnectionMultiplexer _redis;

        private readonly RedisLoginRateLimitService _loginRateLimit;
        private readonly RedisLoginAttemptService _loginAttempt;

        private readonly JwtService _jwtService;
        private readonly RefreshTokenService _refreshTokenService;

        private readonly ILogger<AuthController> _logger;
        private readonly JwtSettings _jwtSettings;
        private readonly IWebHostEnvironment _environment;

        // =========================================================
        // CONSTANTS
        // =========================================================

        private const int MaxFullNameLength = 100;

        private const int MinPasswordLength = 8;
        private const int MaxPasswordLength = 128;

        private const int SecurityCodeLength = 4;

        private const int CaptchaLength = 6;

        private const int MaxForgotPasswordAttempts = 5;

        private const int MaxVerifyRegisterAttempts = 5;

        private const int MaxCaptchaRequests = 20;

        private static readonly TimeSpan RegistrationLifetime =
            TimeSpan.FromMinutes(5);

        private static readonly TimeSpan CaptchaLifetime =
            TimeSpan.FromMinutes(5);

        private static readonly TimeSpan PasswordResetLifetime =
            TimeSpan.FromMinutes(10);

        private static readonly TimeSpan ForgotPasswordRateLimitLifetime =
            TimeSpan.FromMinutes(10);

        private static readonly TimeSpan VerifyRegisterRateLimitLifetime =
            TimeSpan.FromMinutes(10);

        private static readonly TimeSpan CaptchaRateLimitLifetime =
            TimeSpan.FromMinutes(10);

        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public AuthController(
            CodinoDbContext context,
            IDistributedCache cache,
            IConnectionMultiplexer redis,
            RedisLoginRateLimitService loginRateLimit,
            RedisLoginAttemptService loginAttempt,
            JwtService jwtService,
            RefreshTokenService refreshTokenService,
            IOptions<JwtSettings> jwtSettings,
            ILogger<AuthController> logger,
            IWebHostEnvironment environment)
        {
            _context = context;
            _cache = cache;
            _redis = redis;

            _loginRateLimit = loginRateLimit;
            _loginAttempt = loginAttempt;

            _jwtService = jwtService;
            _refreshTokenService = refreshTokenService;

            _jwtSettings = jwtSettings.Value;

            _logger = logger;
            _environment = environment;
        }

        // =========================================================
        // REGISTER
        // POST: api/Auth/register
        // =========================================================

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromBody] RegisterRequest? request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "اطلاعات ثبت‌نام ارسال نشده است."
                    });
                }

                string fullName =
                    NormalizeText(request.FullName);

                string phoneNumber =
                    NormalizePhoneNumber(request.PhoneNumber);

                string password =
                    request.Password ?? string.Empty;

                string repeatPassword =
                    request.RepeatPassword ?? string.Empty;

                // =====================================================
                // VALIDATION
                // =====================================================

                if (string.IsNullOrWhiteSpace(fullName))
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "نام و نام خانوادگی را وارد کنید."
                    });
                }

                if (ContainsDangerousControlCharacters(fullName))
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "نام وارد شده معتبر نیست."
                    });
                }

                if (fullName.Length > MaxFullNameLength)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message =
                            "نام و نام خانوادگی بیش از حد طولانی است."
                    });
                }

                if (!IsValidPhoneNumber(phoneNumber))
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "شماره موبایل معتبر نیست."
                    });
                }

                if (!IsValidPassword(password))
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message =
                            $"رمز عبور باید بین {MinPasswordLength} تا {MaxPasswordLength} کاراکتر باشد."
                    });
                }

                if (password != repeatPassword)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "رمزهای عبور یکسان نیستند."
                    });
                }

                // =====================================================
                // CHECK EXISTING USER
                // =====================================================

                bool exists =
                    await _context.Users
                        .AsNoTracking()
                        .AnyAsync(x =>
                            x.PhoneNumber == phoneNumber);

                if (exists)
                {
                    return Conflict(new
                    {
                        Success = false,
                        Message =
                            "اطلاعات ثبت‌نام معتبر نیست."
                    });
                }

                // =====================================================
                // PASSWORD HASH
                // =====================================================

                var passwordHasher =
                    new PasswordHasher<User>();

                var temporaryUser =
                    new User
                    {
                        FullName = fullName,
                        PhoneNumber = phoneNumber
                    };

                string passwordHash =
                    passwordHasher.HashPassword(
                        temporaryUser,
                        password);

                // =====================================================
                // SECURITY CODES
                // =====================================================

                string securityCode1 =
                    GenerateSecurityCode();

                string securityCode2 =
                    GenerateSecurityCode();

                while (securityCode1 == securityCode2)
                {
                    securityCode2 =
                        GenerateSecurityCode();
                }

                string securityCode1Hash =
                    passwordHasher.HashPassword(
                        temporaryUser,
                        securityCode1);

                string securityCode2Hash =
                    passwordHasher.HashPassword(
                        temporaryUser,
                        securityCode2);

                // =====================================================
                // SAVE PENDING REGISTRATION
                // =====================================================

                var pendingRegistration =
                    new PendingRegistrationSecurityRedis
                    {
                        FullName = fullName,

                        PhoneNumber = phoneNumber,

                        PasswordHash = passwordHash,

                        SecurityCode1Hash =
                            securityCode1Hash,

                        SecurityCode2Hash =
                            securityCode2Hash,

                        ExpiresAt =
                            DateTime.UtcNow.Add(
                                RegistrationLifetime)
                    };

                string json =
                    JsonSerializer.Serialize(
                        pendingRegistration);

                string redisKey =
                    $"register:security:{HashIdentifier(phoneNumber)}";

                await _cache.SetStringAsync(
                    redisKey,
                    json,
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow =
                            RegistrationLifetime
                    });

                // =====================================================
                // RESPONSE
                // =====================================================

                // امنیت مهم:
                // در Production کد امنیتی نباید داخل Response ارسال شود.
                // =====================================================
                // RESPONSE
                // =====================================================

                return Ok(new
                {
                    Success = true,

                    Message =
                        "ثبت‌نام اولیه انجام شد. یکی از کدهای امنیتی را وارد کنید.",

                    PhoneNumber = phoneNumber,

                    SecurityCode1 = securityCode1,

                    SecurityCode2 = securityCode2
                });
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogError(
                    ex,
                    "Redis connection error during registration.");

                return RedisUnavailable(
                    "سرویس Redis در دسترس نیست.");
            }
            catch (RedisTimeoutException ex)
            {
                _logger.LogError(
                    ex,
                    "Redis timeout during registration.");

                return RedisUnavailable(
                    "زمان اتصال به Redis به پایان رسید.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected registration error.");

                return ServerError(
                    "در ثبت‌نام خطایی در سرور رخ داد.");
            }
        }

        // =========================================================
        // VERIFY REGISTER
        // POST: api/Auth/verify-register
        // =========================================================


        [AllowAnonymous]
        [HttpPost("verify-register")]
        public async Task<IActionResult> VerifyRegister(
            [FromBody] VerifyRegisterRequest? request)
        {
            string? redisKey = null;
            string? verifyRateKey = null;
            string? phoneHash = null;

            try
            {
                // 1. اعتبارسنجی درخواست
                if (request == null)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "اطلاعات تأیید ثبت‌نام ارسال نشده است."
                    });
                }

                string phoneNumber = NormalizePhoneNumber(request.PhoneNumber);
                string code = NormalizeDigits(request.Code);

                if (!IsValidPhoneNumber(phoneNumber))
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "اطلاعات ثبت‌نام معتبر نیست."
                    });
                }

                if (!IsValidSecurityCode(code))
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "کد امنیتی باید دقیقاً ۴ رقم باشد."
                    });
                }

                phoneHash = HashIdentifier(phoneNumber);

                // 2. محدودیت تعداد تلاش‌های تأیید
                string ipAddress = GetClientIp();

                verifyRateKey =
                    $"register-verify-rate:{HashIdentifier(ipAddress)}:{phoneHash}";

                long verifyAttempts = await IncrementRedisCounterAsync(
                    verifyRateKey,
                    VerifyRegisterRateLimitLifetime);

                if (verifyAttempts > MaxVerifyRegisterAttempts)
                {
                    return StatusCode(
                        StatusCodes.Status429TooManyRequests,
                        new ApiResponse
                        {
                            Success = false,
                            Message =
                                "تعداد تلاش‌های تأیید بیش از حد مجاز است. لطفاً بعداً دوباره تلاش کنید."
                        });
                }

                // 3. دریافت اطلاعات ثبت‌نام موقت از Redis
                redisKey = $"register:security:{phoneHash}";

                string? json = await _cache.GetStringAsync(redisKey);

                if (string.IsNullOrWhiteSpace(json))
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "درخواست ثبت‌نام پیدا نشد یا منقضی شده است."
                    });
                }

                PendingRegistrationSecurityRedis? pending;

                try
                {
                    pending =
                        JsonSerializer.Deserialize<PendingRegistrationSecurityRedis>(
                            json);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Invalid pending registration data. PhoneHash={PhoneHash}",
                        phoneHash);

                    await TryRemoveRegistrationCacheAsync(redisKey);

                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "اطلاعات ثبت‌نام نامعتبر است. لطفاً دوباره ثبت‌نام کنید."
                    });
                }

                if (pending == null)
                {
                    await TryRemoveRegistrationCacheAsync(redisKey);

                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "اطلاعات ثبت‌نام نامعتبر است."
                    });
                }

                // 4. بررسی انقضای اطلاعات ثبت‌نام
                if (DateTime.UtcNow > pending.ExpiresAt)
                {
                    await TryRemoveRegistrationCacheAsync(redisKey);

                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "کدهای امنیتی منقضی شده‌اند."
                    });
                }

                // 5. بررسی شماره موبایل
                string pendingPhoneNumber =
                    NormalizePhoneNumber(pending.PhoneNumber);

                if (!string.Equals(
                        pendingPhoneNumber,
                        phoneNumber,
                        StringComparison.Ordinal))
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "اطلاعات ثبت‌نام معتبر نیست."
                    });
                }

                // 6. بررسی کامل بودن اطلاعات موقت
                if (string.IsNullOrWhiteSpace(pending.FullName) ||
                    string.IsNullOrWhiteSpace(pending.PasswordHash) ||
                    string.IsNullOrWhiteSpace(pending.SecurityCode1Hash) ||
                    string.IsNullOrWhiteSpace(pending.SecurityCode2Hash))
                {
                    _logger.LogWarning(
                        "Incomplete pending registration. PhoneHash={PhoneHash}",
                        phoneHash);

                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "اطلاعات ثبت‌نام ناقص است. لطفاً دوباره ثبت‌نام کنید."
                    });
                }

                // 7. بررسی کد امنیتی
                var passwordHasher = new PasswordHasher<User>();

                var temporaryUser = new User
                {
                    PhoneNumber = phoneNumber
                };

                bool code1Valid = VerifySecurityHash(
                    passwordHasher,
                    temporaryUser,
                    pending.SecurityCode1Hash,
                    code);

                bool code2Valid = VerifySecurityHash(
                    passwordHasher,
                    temporaryUser,
                    pending.SecurityCode2Hash,
                    code);

                if (!code1Valid && !code2Valid)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "کد امنیتی اشتباه است."
                    });
                }

                // 8. بررسی شماره تکراری در SQL Server
                bool exists;

                try
                {
                    exists = await _context.Users
                        .AsNoTracking()
                        .AnyAsync(x => x.PhoneNumber == phoneNumber);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Database query failed during registration verification. " +
                        "PhoneHash={PhoneHash}",
                        phoneHash);

                    return ServerError(
                        "بررسی اطلاعات ثبت‌نام در پایگاه داده ناموفق بود.");
                }

                if (exists)
                {
                    await TryRemoveRegistrationCacheAsync(redisKey);

                    return Conflict(new ApiResponse
                    {
                        Success = false,
                        Message = "این شماره قبلاً ثبت‌نام کرده است."
                    });
                }

                // 9. ساخت کاربر جدید
                var user = new User
                {
                    FullName = pending.FullName.Trim(),
                    PhoneNumber = phoneNumber,
                    PasswordHash = pending.PasswordHash,
                    SecurityCode1Hash = pending.SecurityCode1Hash,
                    SecurityCode2Hash = pending.SecurityCode2Hash,
                    IsPhoneVerified = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);

                // 10. ذخیره کاربر در SQL Server
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException ex)
                {
                    Exception rootException = ex.GetBaseException();

                    _logger.LogError(
                        ex,
                        "Database update failed during registration. " +
                        "PhoneHash={PhoneHash}; " +
                        "RootExceptionType={RootExceptionType}; " +
                        "RootExceptionMessage={RootExceptionMessage}",
                        phoneHash,
                        rootException.GetType().FullName,
                        rootException.Message);

                    _context.Entry(user).State = EntityState.Detached;

                    return Conflict(new ApiResponse
                    {
                        Success = false,
                        Message = "ثبت‌نام انجام نشد. لطفاً اطلاعات را بررسی و دوباره تلاش کنید."
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Unexpected database save error during registration. " +
                        "PhoneHash={PhoneHash}",
                        phoneHash);

                    _context.Entry(user).State = EntityState.Detached;

                    return ServerError(
                        "ذخیره اطلاعات ثبت‌نام ناموفق بود. لطفاً دوباره تلاش کنید.");
                }

                // 11. پاک‌سازی اطلاعات موقت بعد از ذخیره موفق
                await TryRemoveRegistrationCacheAsync(redisKey);

                if (!string.IsNullOrWhiteSpace(verifyRateKey))
                {
                    await TryRemoveRegistrationCacheAsync(verifyRateKey);
                }

                // 12. پاسخ موفقیت
                _logger.LogInformation(
                    "Registration completed. UserId={UserId}",
                    user.Id);

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "ثبت‌نام با موفقیت تکمیل شد.",
                    UserId = user.Id
                });
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogError(
                    ex,
                    "Redis connection error during registration verification. " +
                    "PhoneHash={PhoneHash}",
                    phoneHash);

                return RedisUnavailable("سرویس Redis در دسترس نیست.");
            }
            catch (RedisTimeoutException ex)
            {
                _logger.LogError(
                    ex,
                    "Redis timeout during registration verification. " +
                    "PhoneHash={PhoneHash}",
                    phoneHash);

                return RedisUnavailable("زمان اتصال به Redis به پایان رسید.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error during registration verification. " +
                    "PhoneHash={PhoneHash}",
                    phoneHash);

                return ServerError("در تأیید ثبت‌نام خطایی در سرور رخ داد.");
            }
        }
        
        private async Task TryRemoveRegistrationCacheAsync(string key)
        {
            try
            {
                await _cache.RemoveAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to remove temporary registration cache.");
            }
        }


        // =========================================================
        // LOGIN
        // POST: api/Auth/login
        // =========================================================

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequest? request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new LoginResponse
                    {
                        Success = false,
                        Message =
                            "اطلاعات ورود ارسال نشده است."
                    });
                }

                string ipAddress =
                    GetClientIp();

                string phoneNumber =
                    NormalizePhoneNumber(
                        request.PhoneNumber);

                string password =
                    request.Password ??
                    string.Empty;

                // =====================================================
                // VALIDATION
                // =====================================================

                if (!IsValidPhoneNumber(phoneNumber))
                {
                    return BadRequest(new LoginResponse
                    {
                        Success = false,
                        Message =
                            "شماره موبایل معتبر نیست."
                    });
                }

                if (string.IsNullOrWhiteSpace(password))
                {
                    return BadRequest(new LoginResponse
                    {
                        Success = false,
                        Message =
                            "رمز عبور را وارد کنید."
                    });
                }

                if (password.Length >
                    MaxPasswordLength)
                {
                    return BadRequest(new LoginResponse
                    {
                        Success = false,
                        Message =
                            "رمز عبور بیش از حد طولانی است."
                    });
                }

                // =====================================================
                // RATE LIMIT
                // =====================================================

                bool allowed =
                    await _loginRateLimit
                        .IsAllowedAsync(
                            ipAddress);

                if (!allowed)
                {
                    return StatusCode(
                        StatusCodes.Status429TooManyRequests,
                        new LoginResponse
                        {
                            Success = false,
                            Message =
                                "تعداد تلاش‌های ورود بیش از حد مجاز است. کمی بعد دوباره تلاش کنید."
                        });
                }

                // =====================================================
                // LOGIN LOCK
                // =====================================================

                TimeSpan? lockTime =
                    await _loginAttempt
                        .GetLockTimeAsync(
                            ipAddress,
                            phoneNumber);

                if (lockTime.HasValue)
                {
                    int minutes =
                        Math.Max(
                            1,
                            (int)Math.Ceiling(
                                lockTime.Value.TotalMinutes));

                    return StatusCode(
                        StatusCodes.Status423Locked,
                        new LoginResponse
                        {
                            Success = false,
                            Message =
                                $"ورود موقتاً قفل شده است. حدود {minutes} دقیقه دیگر دوباره تلاش کنید."
                        });
                }

                // =====================================================
                // FIND USER
                // =====================================================

                var user =
                    await _context.Users
                        .FirstOrDefaultAsync(
                            x =>
                                x.PhoneNumber ==
                                phoneNumber);

                var passwordHasher =
                    new PasswordHasher<User>();

                // =====================================================
                // NON EXISTING USER
                // =====================================================

                if (user == null)
                {
                    // برای جلوگیری از اختلاف زمانی
                    // PasswordHasher را حتی برای User غیرموجود
                    // اجرا می‌کنیم.
                    var fakeUser =
                        new User
                        {
                            PhoneNumber =
                                phoneNumber
                        };

                    string dummyHash =
                        passwordHasher.HashPassword(
                            fakeUser,
                            "DummyPassword-DoNotUse-123!");

                    passwordHasher.VerifyHashedPassword(
                        fakeUser,
                        dummyHash,
                        password);

                    await _loginAttempt.RegisterFailedAttemptAsync(
      ipAddress,
      phoneNumber
  );

                    return Unauthorized(
                        new LoginResponse
                        {
                            Success = false,
                            Message =
                                "شماره موبایل یا رمز عبور اشتباه است."
                        });
                }

                // =====================================================
                // PHONE VERIFIED
                // =====================================================

                if (!user.IsPhoneVerified)
                {
                    return Unauthorized(
                        new LoginResponse
                        {
                            Success = false,
                            Message =
                                "شماره موبایل یا رمز عبور اشتباه است."
                        });
                }

                // =====================================================
                // PASSWORD VERIFY
                // =====================================================

                PasswordVerificationResult result;

                try
                {
                    result =
                        passwordHasher
                            .VerifyHashedPassword(
                                user,
                                user.PasswordHash,
                                password);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Password verification failed. UserId={UserId}",
                        user.Id);

                    return ServerError(
                        "خطایی هنگام بررسی رمز عبور رخ داد.");
                }

                // =====================================================
                // WRONG PASSWORD
                // =====================================================

                if (result ==
                    PasswordVerificationResult.Failed)
                {
                    int failedAttempts =
       await _loginAttempt.RegisterFailedAttemptAsync(
           ipAddress,
           phoneNumber
       );

                    if (failedAttempts >= 3)
                    {
                        return StatusCode(
                            StatusCodes.Status423Locked,
                            new LoginResponse
                            {
                                Success = false,
                                Message =
                                    "به دلیل چند تلاش ناموفق، ورود شما موقتاً قفل شد."
                            });
                    }

                    return Unauthorized(
                        new LoginResponse
                        {
                            Success = false,
                            Message =
                                "شماره موبایل یا رمز عبور اشتباه است."
                        });
                }

                // =====================================================
                // REHASH
                // =====================================================

                if (result ==
                    PasswordVerificationResult.SuccessRehashNeeded)
                {
                    user.PasswordHash =
                        passwordHasher.HashPassword(
                            user,
                            password);

                    await _context.SaveChangesAsync();
                }

                // =====================================================
                // CLEAR FAILED ATTEMPTS
                // =====================================================

                await _loginAttempt
                    .ClearAttemptsAsync(
                        ipAddress,
                        phoneNumber);

                // =====================================================
                // CREATE ACCESS TOKEN
                // =====================================================

                string accessToken =
                    _jwtService.CreateAccessToken(
                        user);

                // =====================================================
                // CREATE REFRESH TOKEN
                // =====================================================

                string refreshToken =
                    _jwtService.CreateRefreshToken();

                await _refreshTokenService.SaveAsync(
                    user.Id,
                    refreshToken);

                _logger.LogInformation(
                    "Successful login. UserId={UserId}, IP={IP}",
                    user.Id,
                    ipAddress);

                // =====================================================
                // RESPONSE
                // =====================================================

                return Ok(new LoginResponse
                {
                    Success = true,

                    Message =
                        "ورود با موفقیت انجام شد.",

                    UserId =
                        user.Id,

                    FullName =
                        user.FullName,

                    AccessToken =
                        accessToken,

                    RefreshToken =
                        refreshToken,

                    AccessTokenExpiresAt =
                        DateTime.UtcNow.AddMinutes(
                            _jwtSettings.AccessTokenMinutes)
                });
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogError(
                    ex,
                    "Redis connection error during login.");

                return RedisUnavailable(
                    "اتصال به Redis برقرار نیست.");
            }
            catch (RedisTimeoutException ex)
            {
                _logger.LogError(
                    ex,
                    "Redis timeout during login.");

                return RedisUnavailable(
                    "زمان اتصال به Redis به پایان رسید.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected login error.");

                return ServerError(
                    "خطایی هنگام ورود رخ داد.");
            }
        }

        // =========================================================
        // CAPTCHA
        // GET: api/Auth/captcha
        // =========================================================

        [AllowAnonymous]
        [HttpGet("captcha")]
        public async Task<IActionResult> GetCaptcha()
        {
            try
            {
                string ipAddress =
                    GetClientIp();

                string rateKey =
                    $"captcha-rate:{HashIdentifier(ipAddress)}";

                long requests =
                    await IncrementRedisCounterAsync(
                        rateKey,
                        CaptchaRateLimitLifetime);

                if (requests >
                    MaxCaptchaRequests)
                {
                    return StatusCode(
                        StatusCodes.Status429TooManyRequests,
                        new CaptchaResponse
                        {
                            Success = false,
                            Message =
                                "تعداد درخواست کپچا بیش از حد مجاز است."
                        });
                }

                string captchaId =
                    Guid.NewGuid().ToString("N");

                const string characters =
                    "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

                char[] captchaChars =
                    new char[CaptchaLength];

                for (int i = 0;
                    i < captchaChars.Length;
                    i++)
                {
                    captchaChars[i] =
                        characters[
                            RandomNumberGenerator.GetInt32(
                                characters.Length)];
                }

                string captchaCode =
                    new string(captchaChars);

                var captchaData =
                    new CaptchaRedis
                    {
                        Code =
                            captchaCode,

                        ExpiresAt =
                            DateTime.UtcNow.Add(
                                CaptchaLifetime)
                    };

                string json =
                    JsonSerializer.Serialize(
                        captchaData);

                string redisKey =
                    $"captcha:{captchaId}";

                await _cache.SetStringAsync(
                    redisKey,
                    json,
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow =
                            CaptchaLifetime
                    });

                string image =
                    CreateCaptchaSvg(
                        captchaCode);

                return Ok(new CaptchaResponse
                {
                    Success = true,

                    CaptchaId =
                        captchaId,

                    Image =
                        image,

                    Message =
                        "کپچا با موفقیت ایجاد شد."
                });
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogError(
                    ex,
                    "Redis error during captcha.");

                return RedisUnavailable(
                    "سرویس Redis در دسترس نیست.");
            }
            catch (RedisTimeoutException ex)
            {
                _logger.LogError(
                    ex,
                    "Redis timeout during captcha.");

                return RedisUnavailable(
                    "زمان اتصال به Redis به پایان رسید.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Captcha generation error.");

                return ServerError(
                    "خطایی هنگام ایجاد کپچا رخ داد.");
            }
        }

        // =========================================================
        // FORGOT PASSWORD
        // POST: api/Auth/forgot-password
        // =========================================================

        [AllowAnonymous]
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(
            [FromBody]
            ForgotPasswordSecurityRequest? request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(
                        new ForgotPasswordResponse
                        {
                            Success = false,
                            Message =
                                "اطلاعات بازیابی رمز عبور ارسال نشده است."
                        });
                }

                string phoneNumber =
                    NormalizePhoneNumber(
                        request.PhoneNumber);

                string securityCode =
                    NormalizeDigits(
                        request.SecurityCode);

                string captchaId =
                    NormalizeText(
                        request.CaptchaId);

                string captcha =
                    NormalizeText(
                        request.Captcha)
                    .ToUpperInvariant();

                string ipAddress =
                    GetClientIp();

                // =====================================================
                // VALIDATION
                // =====================================================

                if (!IsValidPhoneNumber(phoneNumber) ||
                    !IsValidSecurityCode(securityCode))
                {
                    return BadRequest(
                        new ForgotPasswordResponse
                        {
                            Success = false,
                            Message =
                                "اطلاعات بازیابی صحیح نیست."
                        });
                }

                if (string.IsNullOrWhiteSpace(captchaId) ||
                    string.IsNullOrWhiteSpace(captcha))
                {
                    return BadRequest(
                        new ForgotPasswordResponse
                        {
                            Success = false,
                            Message =
                                "اطلاعات کپچا کامل نیست."
                        });
                }

                // =====================================================
                // RATE LIMIT
                // =====================================================

                string rateLimitKey =
                    $"forgot-password-rate:{HashIdentifier(ipAddress)}:{HashIdentifier(phoneNumber)}";

                long attempts =
                    await IncrementRedisCounterAsync(
                        rateLimitKey,
                        ForgotPasswordRateLimitLifetime);

                if (attempts >
                    MaxForgotPasswordAttempts)
                {
                    return StatusCode(
                        StatusCodes.Status429TooManyRequests,
                        new ForgotPasswordResponse
                        {
                            Success = false,
                            Message =
                                "تعداد تلاش‌های بازیابی بیش از حد مجاز است. لطفاً بعداً دوباره تلاش کنید."
                        });
                }

                // =====================================================
                // GET CAPTCHA
                // =====================================================

                string captchaRedisKey =
                    $"captcha:{captchaId}";

                string? captchaJson =
                    await _cache.GetStringAsync(
                        captchaRedisKey);

                if (string.IsNullOrWhiteSpace(captchaJson))
                {
                    return BadRequest(
                        new ForgotPasswordResponse
                        {
                            Success = false,
                            Message =
                                "کپچا منقضی شده است."
                        });
                }

                CaptchaRedis? captchaData;

                try
                {
                    captchaData =
                        JsonSerializer.Deserialize<
                            CaptchaRedis>(
                                captchaJson);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Invalid captcha JSON.");

                    await _cache.RemoveAsync(
                        captchaRedisKey);

                    return BadRequest(
                        new ForgotPasswordResponse
                        {
                            Success = false,
                            Message =
                                "اطلاعات کپچا نامعتبر است."
                        });
                }

                if (captchaData == null)
                {
                    await _cache.RemoveAsync(
                        captchaRedisKey);

                    return BadRequest(
                        new ForgotPasswordResponse
                        {
                            Success = false,
                            Message =
                                "اطلاعات کپچا نامعتبر است."
                        });
                }

                // =====================================================
                // CAPTCHA EXPIRATION
                // =====================================================

                if (DateTime.UtcNow >
                    captchaData.ExpiresAt)
                {
                    await _cache.RemoveAsync(
                        captchaRedisKey);

                    return BadRequest(
                        new ForgotPasswordResponse
                        {
                            Success = false,
                            Message =
                                "کپچا منقضی شده است."
                        });
                }

                // =====================================================
                // CAPTCHA VERIFY
                // =====================================================

                if (!CryptographicEquals(
                        captchaData.Code,
                        captcha))
                {
                    await _cache.RemoveAsync(
                        captchaRedisKey);

                    return BadRequest(
                        new ForgotPasswordResponse
                        {
                            Success = false,
                            Message =
                                "کپچا اشتباه است."
                        });
                }

                await _cache.RemoveAsync(
                    captchaRedisKey);

                // =====================================================
                // FIND USER
                // =====================================================

                var user =
                    await _context.Users
                        .FirstOrDefaultAsync(
                            x =>
                                x.PhoneNumber ==
                                phoneNumber);

                if (user == null)
                {
                    return BadRequest(
                        new ForgotPasswordResponse
                        {
                            Success = false,
                            Message =
                                "اطلاعات بازیابی صحیح نیست."
                        });
                }

                // =====================================================
                // VERIFY SECURITY CODE
                // =====================================================

                var passwordHasher =
                    new PasswordHasher<User>();

                bool code1Valid =
                    VerifySecurityHash(
                        passwordHasher,
                        user,
                        user.SecurityCode1Hash,
                        securityCode);

                bool code2Valid =
                    VerifySecurityHash(
                        passwordHasher,
                        user,
                        user.SecurityCode2Hash,
                        securityCode);

                if (!code1Valid && !code2Valid)
                {
                    return BadRequest(
                        new ForgotPasswordResponse
                        {
                            Success = false,
                            Message =
                                "کد امنیتی اشتباه است."
                        });
                }

                // =====================================================
                // CREATE RESET TOKEN
                // =====================================================

                string resetToken =
                    GenerateSecureToken();

                var resetData =
                    new PasswordResetRedis
                    {
                        PhoneNumber =
                            phoneNumber,

                        ExpiresAt =
                            DateTime.UtcNow.Add(
                                PasswordResetLifetime)
                    };

                string resetJson =
                    JsonSerializer.Serialize(
                        resetData);

                string resetRedisKey =
                    $"password-reset:{resetToken}";

                await _cache.SetStringAsync(
                    resetRedisKey,
                    resetJson,
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow =
                            PasswordResetLifetime
                    });

                return Ok(
                    new ForgotPasswordResponse
                    {
                        Success = true,

                        Message =
                            "کد امنیتی صحیح است. اکنون می‌توانید رمز عبور را تغییر دهید.",

                        ResetToken =
                            resetToken
                    });
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogError(
                    ex,
                    "Redis error during forgot password.");

                return RedisUnavailable(
                    "سرویس Redis در دسترس نیست.");
            }
            catch (RedisTimeoutException ex)
            {
                _logger.LogError(
                    ex,
                    "Redis timeout during forgot password.");

                return RedisUnavailable(
                    "زمان اتصال به Redis به پایان رسید.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Forgot password error.");

                return ServerError(
                    "خطایی هنگام بازیابی رمز عبور رخ داد.");
            }
        }

        // =========================================================
        // RESET PASSWORD
        // POST: api/Auth/reset-password
        // =========================================================

        [AllowAnonymous]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(
            [FromBody]
            ResetPasswordRequest? request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            Message =
                                "اطلاعات تغییر رمز عبور ارسال نشده است."
                        });
                }

                string phoneNumber =
                    NormalizePhoneNumber(
                        request.PhoneNumber);

                string resetToken =
                    NormalizeText(
                        request.ResetToken);

                string newPassword =
                    request.NewPassword ??
                    string.Empty;

                string repeatPassword =
                    request.RepeatPassword ??
                    string.Empty;

                // =====================================================
                // VALIDATION
                // =====================================================

                if (!IsValidPhoneNumber(phoneNumber))
                {
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            Message =
                                "اطلاعات بازیابی معتبر نیست."
                        });
                }

                if (string.IsNullOrWhiteSpace(resetToken) ||
                    resetToken.Length > 128)
                {
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            Message =
                                "توکن بازیابی معتبر نیست."
                        });
                }

                if (!IsValidPassword(newPassword))
                {
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            Message =
                                $"رمز عبور باید بین {MinPasswordLength} تا {MaxPasswordLength} کاراکتر باشد."
                        });
                }

                if (newPassword != repeatPassword)
                {
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            Message =
                                "رمزهای عبور یکسان نیستند."
                        });
                }

                // =====================================================
                // GET RESET TOKEN
                // =====================================================

                string redisKey =
                    $"password-reset:{resetToken}";

                string? json =
                    await _cache.GetStringAsync(
                        redisKey);

                if (string.IsNullOrWhiteSpace(json))
                {
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            Message =
                                "درخواست بازیابی منقضی شده یا معتبر نیست."
                        });
                }

                PasswordResetRedis? resetData;

                try
                {
                    resetData =
                        JsonSerializer.Deserialize<
                            PasswordResetRedis>(
                                json);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Invalid password reset JSON.");

                    await _cache.RemoveAsync(
                        redisKey);

                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            Message =
                                "اطلاعات بازیابی نامعتبر است."
                        });
                }

                if (resetData == null)
                {
                    await _cache.RemoveAsync(
                        redisKey);

                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            Message =
                                "اطلاعات بازیابی نامعتبر است."
                        });
                }

                // =====================================================
                // EXPIRATION
                // =====================================================

                if (DateTime.UtcNow >
                    resetData.ExpiresAt)
                {
                    await _cache.RemoveAsync(
                        redisKey);

                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            Message =
                                "زمان بازیابی رمز عبور به پایان رسیده است."
                        });
                }

                // =====================================================
                // CHECK PHONE
                // =====================================================

                if (!string.Equals(
                        resetData.PhoneNumber,
                        phoneNumber,
                        StringComparison.Ordinal))
                {
                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            Message =
                                "درخواست بازیابی معتبر نیست."
                        });
                }

                // =====================================================
                // FIND USER
                // =====================================================

                var user =
                    await _context.Users
                        .FirstOrDefaultAsync(
                            x =>
                                x.PhoneNumber ==
                                phoneNumber);

                if (user == null)
                {
                    await _cache.RemoveAsync(
                        redisKey);

                    return BadRequest(
                        new ApiResponse
                        {
                            Success = false,
                            Message =
                                "درخواست بازیابی معتبر نیست."
                        });
                }

                // =====================================================
                // HASH NEW PASSWORD
                // =====================================================

                var passwordHasher =
                    new PasswordHasher<User>();

                user.PasswordHash =
                    passwordHasher.HashPassword(
                        user,
                        newPassword);

                // =====================================================
                // SECURITY CODES INVALIDATED
                // =====================================================

                user.SecurityCode1Hash =
                    string.Empty;

                user.SecurityCode2Hash =
                    string.Empty;

                await _context.SaveChangesAsync();

                // =====================================================
                // ONE-TIME RESET TOKEN
                // =====================================================

                await _cache.RemoveAsync(
                    redisKey);

                // =====================================================
                // CLEAR RATE LIMIT
                // =====================================================

                string ipAddress =
                    GetClientIp();

                string rateLimitKey =
                    $"forgot-password-rate:{HashIdentifier(ipAddress)}:{HashIdentifier(phoneNumber)}";

                await _cache.RemoveAsync(
                    rateLimitKey);

                _logger.LogInformation(
                    "Password reset successful. UserId={UserId}",
                    user.Id);

                return Ok(
                    new ApiResponse
                    {
                        Success = true,

                        Message =
                            "رمز عبور با موفقیت تغییر کرد."
                    });
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogError(
                    ex,
                    "Redis error during reset password.");

                return RedisUnavailable(
                    "سرویس Redis در دسترس نیست.");
            }
            catch (RedisTimeoutException ex)
            {
                _logger.LogError(
                    ex,
                    "Redis timeout during reset password.");

                return RedisUnavailable(
                    "زمان اتصال به Redis به پایان رسید.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Reset password error.");

                return ServerError(
                    "خطایی هنگام تغییر رمز عبور رخ داد.");
            }
        }

        // =========================================================
        // CAPTCHA SVG
        // =========================================================

        private static string CreateCaptchaSvg(
            string code)
        {
            const int width = 240;
            const int height = 90;

            var svg =
                new StringBuilder();

            svg.Append($"""
                <svg xmlns="http://www.w3.org/2000/svg"
                     width="{width}"
                     height="{height}"
                     viewBox="0 0 {width} {height}">

                <rect
                    width="100%"
                    height="100%"
                    rx="12"
                    fill="#111827"/>
                """);

            // =====================================================
            // NOISE LINES
            // =====================================================

            for (int i = 0; i < 7; i++)
            {
                int x1 =
                    RandomNumberGenerator.GetInt32(
                        0,
                        width);

                int y1 =
                    RandomNumberGenerator.GetInt32(
                        0,
                        height);

                int x2 =
                    RandomNumberGenerator.GetInt32(
                        0,
                        width);

                int y2 =
                    RandomNumberGenerator.GetInt32(
                        0,
                        height);

                svg.Append($"""
                    <line
                        x1="{x1}"
                        y1="{y1}"
                        x2="{x2}"
                        y2="{y2}"
                        stroke="#6b7280"
                        stroke-width="2"
                        opacity="0.7"/>
                    """);
            }

            // =====================================================
            // CHARACTERS
            // =====================================================

            for (int i = 0;
                i < code.Length;
                i++)
            {
                int x =
                    20 + (i * 36);

                int y =
                    RandomNumberGenerator.GetInt32(
                        52,
                        72);

                int rotation =
                    RandomNumberGenerator.GetInt32(
                        -15,
                        16);

                svg.Append($"""
                    <text
                        x="{x}"
                        y="{y}"
                        font-family="Arial"
                        font-size="32"
                        font-weight="bold"
                        fill="#ffffff"
                        transform="rotate({rotation} {x} {y})">
                        {code[i]}
                    </text>
                    """);
            }

            // =====================================================
            // NOISE DOTS
            // =====================================================

            for (int i = 0; i < 40; i++)
            {
                int x =
                    RandomNumberGenerator.GetInt32(
                        0,
                        width);

                int y =
                    RandomNumberGenerator.GetInt32(
                        0,
                        height);

                svg.Append($"""
                    <circle
                        cx="{x}"
                        cy="{y}"
                        r="1.5"
                        fill="#9ca3af"/>
                    """);
            }

            svg.Append("</svg>");

            byte[] bytes =
                Encoding.UTF8.GetBytes(
                    svg.ToString());

            return Convert.ToBase64String(
                bytes);
        }

        // =========================================================
        // FAILED LOGIN
        // =========================================================


       

        // =========================================================
        // PASSWORD VALIDATION
        // =========================================================

        private static bool IsValidPassword(
            string password)
        {
            if (string.IsNullOrEmpty(password))
                return false;

            if (password.Length < MinPasswordLength)
                return false;

            if (password.Length > MaxPasswordLength)
                return false;

            // NUL character نباید وارد سیستم شود.
            if (password.Contains('\0'))
                return false;

            return true;
        }

        // =========================================================
        // SECURITY CODE VALIDATION
        // =========================================================

        private static bool IsValidSecurityCode(
            string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            code =
                NormalizeDigits(code);

            return
                code.Length == SecurityCodeLength &&
                code.All(char.IsDigit);
        }

        // =========================================================
        // VERIFY SECURITY HASH
        // =========================================================

        private static bool VerifySecurityHash(
            PasswordHasher<User> passwordHasher,
            User user,
            string? hash,
            string code)
        {
            if (string.IsNullOrWhiteSpace(hash))
                return false;

            try
            {
                PasswordVerificationResult result =
                    passwordHasher
                        .VerifyHashedPassword(
                            user,
                            hash,
                            code);

                return
                    result !=
                    PasswordVerificationResult.Failed;
            }
            catch
            {
                return false;
            }
        }

        // =========================================================
        // SECURE TOKEN
        // =========================================================

        private static string GenerateSecureToken()
        {
            byte[] bytes =
                RandomNumberGenerator.GetBytes(32);

            return Convert
                .ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }

        // =========================================================
        // SECURITY CODE
        // =========================================================

        private static string GenerateSecurityCode()
        {
            int number =
                RandomNumberGenerator.GetInt32(
                    1000,
                    10000);

            return number.ToString();
        }

        // =========================================================
        // CONSTANT-TIME COMPARE
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

            if (aBytes.Length != bBytes.Length)
                return false;

            return CryptographicOperations
                .FixedTimeEquals(
                    aBytes,
                    bBytes);
        }

        // =========================================================
        // HASH IDENTIFIER
        // =========================================================

        private static string HashIdentifier(
            string value)
        {
            byte[] bytes =
                Encoding.UTF8.GetBytes(
                    value ?? string.Empty);

            byte[] hash =
                SHA256.HashData(bytes);

            return Convert
                .ToHexString(hash)
                .ToLowerInvariant();
        }

        // =========================================================
        // REDIS COUNTER
        // =========================================================

        private async Task<long> IncrementRedisCounterAsync(
            string key,
            TimeSpan expiration)
        {
            IDatabase database =
                _redis.GetDatabase();

            long value =
                await database.StringIncrementAsync(
                    key);

            if (value == 1)
            {
                await database.KeyExpireAsync(
                    key,
                    expiration);
            }

            return value;
        }

        // =========================================================
        // CLIENT IP
        // =========================================================

        private string GetClientIp()
        {
            return
                HttpContext.Connection
                    .RemoteIpAddress?
                    .ToString()
                ?? "unknown";
        }

        // =========================================================
        // PHONE NORMALIZATION
        // =========================================================

        private static string NormalizePhoneNumber(
            string? phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return string.Empty;
            }

            string value =
                phoneNumber.Trim();

            value =
                NormalizeDigits(value);

            value =
                value
                    .Replace(" ", "")
                    .Replace("-", "")
                    .Replace("(", "")
                    .Replace(")", "");

            if (value.StartsWith("+98"))
            {
                value =
                    "0" +
                    value.Substring(3);
            }
            else if (
                value.StartsWith("98") &&
                value.Length == 12)
            {
                value =
                    "0" +
                    value.Substring(2);
            }

            return value;
        }

        // =========================================================
        // TEXT NORMALIZATION
        // =========================================================

        private static string NormalizeText(
            string? value)
        {
            return value?.Trim() ??
                   string.Empty;
        }

        // =========================================================
        // CONTROL CHARACTER CHECK
        // =========================================================

        private static bool ContainsDangerousControlCharacters(
            string value)
        {
            foreach (char c in value)
            {
                if (char.IsControl(c) &&
                    c != '\t')
                {
                    return true;
                }
            }

            return false;
        }

        // =========================================================
        // PERSIAN / ARABIC DIGITS
        // =========================================================

        private static string NormalizeDigits(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            StringBuilder result =
                new StringBuilder(
                    value.Length);

            foreach (char c in value)
            {
                switch (c)
                {
                    case '۰':
                        result.Append('0');
                        break;

                    case '۱':
                        result.Append('1');
                        break;

                    case '۲':
                        result.Append('2');
                        break;

                    case '۳':
                        result.Append('3');
                        break;

                    case '۴':
                        result.Append('4');
                        break;

                    case '۵':
                        result.Append('5');
                        break;

                    case '۶':
                        result.Append('6');
                        break;

                    case '۷':
                        result.Append('7');
                        break;

                    case '۸':
                        result.Append('8');
                        break;

                    case '۹':
                        result.Append('9');
                        break;

                    case '٠':
                        result.Append('0');
                        break;

                    case '١':
                        result.Append('1');
                        break;

                    case '٢':
                        result.Append('2');
                        break;

                    case '٣':
                        result.Append('3');
                        break;

                    case '٤':
                        result.Append('4');
                        break;

                    case '٥':
                        result.Append('5');
                        break;

                    case '٦':
                        result.Append('6');
                        break;

                    case '٧':
                        result.Append('7');
                        break;

                    case '٨':
                        result.Append('8');
                        break;

                    case '٩':
                        result.Append('9');
                        break;

                    default:
                        result.Append(c);
                        break;
                }
            }

            return result.ToString();
        }

        // =========================================================
        // PHONE VALIDATION
        // =========================================================

        private static bool IsValidPhoneNumber(
            string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return false;
            }

            phoneNumber =
                NormalizePhoneNumber(
                    phoneNumber);

            if (phoneNumber.Length != 11)
                return false;

            if (!phoneNumber.StartsWith("09"))
                return false;

            return phoneNumber.All(
                char.IsDigit);
        }

        // =========================================================
        // REDIS ERROR
        // =========================================================

        private ObjectResult RedisUnavailable(
            string message)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new
                {
                    Success = false,
                    Message = message
                });
        }

        // =========================================================
        // SERVER ERROR
        // =========================================================

        private ObjectResult ServerError(
            string message)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    Success = false,
                    Message = message
                });
        }

        // =========================================================
        // PENDING REGISTRATION REDIS MODEL
        // =========================================================

        private sealed class PendingRegistrationSecurityRedis
        {
            public string FullName { get; set; }
                = string.Empty;

            public string PhoneNumber { get; set; }
                = string.Empty;

            public string PasswordHash { get; set; }
                = string.Empty;

            public string SecurityCode1Hash { get; set; }
                = string.Empty;

            public string SecurityCode2Hash { get; set; }
                = string.Empty;

            public DateTime ExpiresAt { get; set; }
        }

        // =========================================================
        // CAPTCHA REDIS MODEL
        // =========================================================

        private sealed class CaptchaRedis
        {
            public string Code { get; set; }
                = string.Empty;

            public DateTime ExpiresAt { get; set; }
        }

        // =========================================================
        // PASSWORD RESET REDIS MODEL
        // =========================================================

        private sealed class PasswordResetRedis
        {
            public string PhoneNumber { get; set; }
                = string.Empty;

            public DateTime ExpiresAt { get; set; }
        }

        // =========================================================
        // FORGOT PASSWORD REQUEST
        // =========================================================

        public sealed class ForgotPasswordSecurityRequest
        {
            public string? PhoneNumber { get; set; }

            public string? SecurityCode { get; set; }

            public string? CaptchaId { get; set; }

            public string? Captcha { get; set; }
        }
    }
}