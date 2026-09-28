using Codino.Api.Data;
using Codino.Api.DTOs;
using Codino.Api.Models;
using Codino.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Codino.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly CodinoDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly RedisLoginRateLimitService _loginRateLimit;
        private readonly RedisLoginAttemptService _loginAttempt;

        public AuthController(
            CodinoDbContext context,
            IDistributedCache cache,
            RedisLoginRateLimitService loginRateLimit,
            RedisLoginAttemptService loginAttempt)
        {
            _context = context;
            _cache = cache;
            _loginRateLimit = loginRateLimit;
            _loginAttempt = loginAttempt;
        }

        // =========================================================
        // REGISTER
        // =========================================================

        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromBody] RegisterRequest request)
        {
            string fullName =
                request.FullName?.Trim() ?? string.Empty;

            string phoneNumber =
                request.PhoneNumber?.Trim() ?? string.Empty;

            string password =
                request.Password ?? string.Empty;

            string repeatPassword =
                request.RepeatPassword ?? string.Empty;

            if (string.IsNullOrWhiteSpace(fullName))
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message =
                        "نام و نام خانوادگی را وارد کنید."
                });
            }

            if (!IsValidPhoneNumber(phoneNumber))
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message =
                        "شماره موبایل معتبر نیست."
                });
            }

            if (password.Length < 8)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message =
                        "رمز عبور باید حداقل ۸ کاراکتر باشد."
                });
            }

            if (password != repeatPassword)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message =
                        "رمزهای عبور یکسان نیستند."
                });
            }

            bool exists =
                await _context.Users.AnyAsync(
                    x => x.PhoneNumber == phoneNumber);

            if (exists)
            {
                return Conflict(new ApiResponse
                {
                    Success = false,
                    Message =
                        "این شماره موبایل قبلاً ثبت‌نام کرده است."
                });
            }

            var user = new User
            {
                FullName = fullName,
                PhoneNumber = phoneNumber,
                IsPhoneVerified = false,
                CreatedAt = DateTime.UtcNow
            };

            var passwordHasher =
                new PasswordHasher<User>();

            user.PasswordHash =
                passwordHasher.HashPassword(
                    user,
                    password);

            const string testOtp = "123456";

            var pendingRegistration =
                new PendingRegistrationRedis
                {
                    FullName = fullName,
                    PhoneNumber = phoneNumber,
                    PasswordHash = user.PasswordHash,
                    Otp = testOtp,
                    OtpExpiresAt =
                        DateTime.UtcNow.AddMinutes(5)
                };

            string json =
                JsonSerializer.Serialize(
                    pendingRegistration);

            string redisKey =
                $"register:otp:{phoneNumber}";

            await _cache.SetStringAsync(
                redisKey,
                json,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow =
                        TimeSpan.FromMinutes(5)
                });

            return Ok(new ApiResponse
            {
                Success = true,
                Message =
                    "ثبت‌نام اولیه انجام شد. کد تأیید را وارد کنید.",
                Otp = testOtp
            });
        }

        // =========================================================
        // VERIFY REGISTER
        // =========================================================

        [HttpPost("verify-register")]
        public async Task<IActionResult> VerifyRegister(
            [FromBody] VerifyRegisterRequest request)
        {
            string phoneNumber =
                request.PhoneNumber?.Trim() ?? string.Empty;

            string code =
                request.Code?.Trim() ?? string.Empty;

            if (!IsValidPhoneNumber(phoneNumber))
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message =
                        "شماره موبایل معتبر نیست."
                });
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message =
                        "کد تأیید را وارد کنید."
                });
            }

            bool exists =
                await _context.Users.AnyAsync(
                    x => x.PhoneNumber == phoneNumber);

            if (exists)
            {
                return Conflict(new ApiResponse
                {
                    Success = false,
                    Message =
                        "این شماره موبایل قبلاً ثبت شده است."
                });
            }

            string redisKey =
                $"register:otp:{phoneNumber}";

            string? json =
                await _cache.GetStringAsync(
                    redisKey);

            if (string.IsNullOrWhiteSpace(json))
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message =
                        "درخواست ثبت‌نام پیدا نشد یا منقضی شده است."
                });
            }

            PendingRegistrationRedis? pending =
                JsonSerializer.Deserialize<PendingRegistrationRedis>(
                    json);

            if (pending == null)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message =
                        "اطلاعات ثبت‌نام نامعتبر است."
                });
            }

            if (DateTime.UtcNow >
                pending.OtpExpiresAt)
            {
                await _cache.RemoveAsync(
                    redisKey);

                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message =
                        "کد تأیید منقضی شده است."
                });
            }

            if (!string.Equals(
                    pending.Otp,
                    code,
                    StringComparison.Ordinal))
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message =
                        "کد تأیید اشتباه است."
                });
            }

            var user = new User
            {
                FullName =
                    pending.FullName,

                PhoneNumber =
                    pending.PhoneNumber,

                PasswordHash =
                    pending.PasswordHash,

                IsPhoneVerified = true,

                CreatedAt =
                    DateTime.UtcNow
            };

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            await _cache.RemoveAsync(
                redisKey);

            return Ok(new ApiResponse
            {
                Success = true,
                Message =
                    "ثبت‌نام با موفقیت تکمیل شد.",
                UserId = user.Id
            });
        }

        // =========================================================
        // LOGIN
        // =========================================================

        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequest request)
        {
            // -----------------------------------------------------
            // GET IP
            // -----------------------------------------------------

            string ipAddress =
                HttpContext.Connection.RemoteIpAddress?.ToString()
                ?? "unknown";

            // -----------------------------------------------------
            // GET PHONE
            // -----------------------------------------------------

            string phoneNumber =
                request.PhoneNumber?.Trim() ?? string.Empty;

            string password =
                request.Password ?? string.Empty;

            // -----------------------------------------------------
            // PHONE VALIDATION
            // -----------------------------------------------------

            if (!IsValidPhoneNumber(phoneNumber))
            {
                return BadRequest(new LoginResponse
                {
                    Success = false,
                    Message =
                        "شماره موبایل معتبر نیست."
                });
            }

            // -----------------------------------------------------
            // PASSWORD VALIDATION
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(password))
            {
                return BadRequest(new LoginResponse
                {
                    Success = false,
                    Message =
                        "رمز عبور را وارد کنید."
                });
            }

            // =====================================================
            // LOGIN RATE LIMIT
            // =====================================================

            bool allowed =
                await _loginRateLimit.IsAllowedAsync(
                    ipAddress);

            if (!allowed)
            {
                return StatusCode(
                    StatusCodes.Status429TooManyRequests,
                    new LoginResponse
                    {
                        Success = false,

                        Message =
                            "شما فقط هر ۲ دقیقه یک بار می‌توانید تلاش کنید. لطفاً کمی بعد دوباره امتحان کنید."
                    });
            }

            // =====================================================
            // CHECK LOGIN LOCK
            // =====================================================

            TimeSpan? lockTime =
                await _loginAttempt.GetLockTimeAsync(
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
                            $"به دلیل چند تلاش ناموفق، ورود شما موقتاً قفل شده است. حدود {minutes} دقیقه دیگر دوباره امتحان کنید."
                    });
            }

            // =====================================================
            // FIND USER
            // =====================================================

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(
                        x => x.PhoneNumber == phoneNumber);

            if (user == null)
            {
                int failedAttempts =
                    await _loginAttempt.RegisterFailedAttemptAsync(
                        ipAddress,
                        phoneNumber);

                if (failedAttempts >= 3)
                {
                    return StatusCode(
                        StatusCodes.Status423Locked,
                        new LoginResponse
                        {
                            Success = false,

                            Message =
                                "به دلیل ۳ تلاش ناموفق، ورود شما برای ۳ دقیقه قفل شد."
                        });
                }

                return Unauthorized(new LoginResponse
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
                return Unauthorized(new LoginResponse
                {
                    Success = false,

                    Message =
                        "شماره موبایل شما تأیید نشده است."
                });
            }

            // =====================================================
            // VERIFY PASSWORD
            // =====================================================

            var passwordHasher =
                new PasswordHasher<User>();

            PasswordVerificationResult passwordResult =
                passwordHasher.VerifyHashedPassword(
                    user,
                    user.PasswordHash,
                    password);

            // =====================================================
            // WRONG PASSWORD
            // =====================================================

            if (passwordResult ==
                PasswordVerificationResult.Failed)
            {
                int failedAttempts =
                    await _loginAttempt.RegisterFailedAttemptAsync(
                        ipAddress,
                        phoneNumber);

                if (failedAttempts >= 3)
                {
                    return StatusCode(
                        StatusCodes.Status423Locked,
                        new LoginResponse
                        {
                            Success = false,

                            Message =
                                "به دلیل ۳ تلاش ناموفق، ورود شما برای ۳ دقیقه قفل شد."
                        });
                }

                return Unauthorized(new LoginResponse
                {
                    Success = false,

                    Message =
                        $"شماره موبایل یا رمز عبور اشتباه است. تلاش ناموفق {failedAttempts} از ۳."
                });
            }

            // =====================================================
            // SUCCESS
            // =====================================================

            await _loginAttempt.ClearAttemptsAsync(
                ipAddress,
                phoneNumber);

            return Ok(new LoginResponse
            {
                Success = true,

                Message =
                    "ورود با موفقیت انجام شد.",

                UserId =
                    user.Id,

                FullName =
                    user.FullName
            });
        }

        // =========================================================
        // GET CAPTCHA
        // =========================================================

        [HttpGet("captcha")]
        public async Task<IActionResult> GetCaptcha()
        {
            string captchaId =
                Guid.NewGuid().ToString("N");

            const string characters =
                "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

            char[] captchaChars =
                new char[6];

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
                    Code = captchaCode,

                    ExpiresAt =
                        DateTime.UtcNow.AddMinutes(5)
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
                        TimeSpan.FromMinutes(5)
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

        // =========================================================
        // CREATE CAPTCHA SVG
        // =========================================================

        private string CreateCaptchaSvg(
            string code)
        {
            const int width = 240;
            const int height = 90;

            StringBuilder svg =
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

            // -----------------------------------------------------
            // RANDOM LINES
            // -----------------------------------------------------

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

            // -----------------------------------------------------
            // CAPTCHA TEXT
            // -----------------------------------------------------

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

            // -----------------------------------------------------
            // RANDOM DOTS
            // -----------------------------------------------------

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
        // FORGOT PASSWORD
        // =========================================================

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(
            [FromBody] ForgotPasswordRequest request)
        {
            string phoneNumber =
                request.PhoneNumber?.Trim() ?? string.Empty;

            string captchaId =
                request.CaptchaId?.Trim() ?? string.Empty;

            string captcha =
                request.Captcha?.Trim()
                    .ToUpperInvariant() ?? string.Empty;

            if (!IsValidPhoneNumber(phoneNumber))
            {
                return BadRequest(
                    new ForgotPasswordResponse
                    {
                        Success = false,
                        Message =
                            "شماره موبایل معتبر نیست."
                    });
            }

            if (string.IsNullOrWhiteSpace(captchaId))
            {
                return BadRequest(
                    new ForgotPasswordResponse
                    {
                        Success = false,
                        Message =
                            "شناسه کپچا ارسال نشده است."
                    });
            }

            if (string.IsNullOrWhiteSpace(captcha))
            {
                return BadRequest(
                    new ForgotPasswordResponse
                    {
                        Success = false,
                        Message =
                            "کپچا را وارد کنید."
                    });
            }

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

            CaptchaRedis? captchaData =
                JsonSerializer.Deserialize<CaptchaRedis>(
                    captchaJson);

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

            if (!string.Equals(
                    captchaData.Code,
                    captcha,
                    StringComparison.OrdinalIgnoreCase))
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

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(
                        x => x.PhoneNumber == phoneNumber);

            if (user == null)
            {
                return NotFound(
                    new ForgotPasswordResponse
                    {
                        Success = false,
                        Message =
                            "کاربری با این شماره موبایل پیدا نشد."
                    });
            }

            string resetToken =
                Convert.ToBase64String(
                    RandomNumberGenerator.GetBytes(32))
                    .Replace("+", "-")
                    .Replace("/", "_")
                    .Replace("=", "");

            var resetData =
                new PasswordResetRedis
                {
                    PhoneNumber =
                        phoneNumber,

                    ExpiresAt =
                        DateTime.UtcNow.AddMinutes(10)
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
                        TimeSpan.FromMinutes(10)
                });

            return Ok(
                new ForgotPasswordResponse
                {
                    Success = true,

                    Message =
                        "کپچا صحیح است.",

                    ResetToken =
                        resetToken
                });
        }

        // =========================================================
        // RESET PASSWORD
        // =========================================================

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(
            [FromBody] ResetPasswordRequest request)
        {
            string phoneNumber =
                request.PhoneNumber?.Trim() ?? string.Empty;

            string resetToken =
                request.ResetToken?.Trim() ?? string.Empty;

            string newPassword =
                request.NewPassword ?? string.Empty;

            string repeatPassword =
                request.RepeatPassword ?? string.Empty;

            if (!IsValidPhoneNumber(phoneNumber))
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message =
                        "شماره موبایل معتبر نیست."
                });
            }

            if (string.IsNullOrWhiteSpace(resetToken))
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message =
                        "توکن بازیابی وارد نشده است."
                });
            }

            if (newPassword.Length < 8)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message =
                        "رمز عبور باید حداقل ۸ کاراکتر باشد."
                });
            }

            if (newPassword != repeatPassword)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message =
                        "رمزهای عبور یکسان نیستند."
                });
            }

            string redisKey =
                $"password-reset:{resetToken}";

            string? json =
                await _cache.GetStringAsync(
                    redisKey);

            if (string.IsNullOrWhiteSpace(json))
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message =
                        "درخواست بازیابی رمز عبور منقضی شده است."
                });
            }

            PasswordResetRedis? resetData =
                JsonSerializer.Deserialize<PasswordResetRedis>(
                    json);

            if (resetData == null)
            {
                await _cache.RemoveAsync(
                    redisKey);

                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message =
                        "اطلاعات بازیابی نامعتبر است."
                });
            }

            if (DateTime.UtcNow >
                resetData.ExpiresAt)
            {
                await _cache.RemoveAsync(
                    redisKey);

                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message =
                        "زمان بازیابی رمز عبور به پایان رسیده است."
                });
            }

            if (!string.Equals(
                    resetData.PhoneNumber,
                    phoneNumber,
                    StringComparison.Ordinal))
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message =
                        "درخواست بازیابی معتبر نیست."
                });
            }

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(
                        x => x.PhoneNumber == phoneNumber);

            if (user == null)
            {
                await _cache.RemoveAsync(
                    redisKey);

                return NotFound(new ApiResponse
                {
                    Success = false,
                    Message =
                        "کاربر پیدا نشد."
                });
            }

            var passwordHasher =
                new PasswordHasher<User>();

            user.PasswordHash =
                passwordHasher.HashPassword(
                    user,
                    newPassword);

            await _context.SaveChangesAsync();

            await _cache.RemoveAsync(
                redisKey);

            return Ok(new ApiResponse
            {
                Success = true,

                Message =
                    "رمز عبور با موفقیت تغییر کرد."
            });
        }

        // =========================================================
        // VALIDATE PHONE NUMBER
        // =========================================================

        private bool IsValidPhoneNumber(
            string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return false;
            }

            return
                phoneNumber.StartsWith("09") &&
                phoneNumber.Length == 11 &&
                phoneNumber.All(char.IsDigit);
        }
    }
}