using Codino.Api.Models;
using Codino.Api.Settings;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Codino.Api.Services
{
    public sealed class JwtService
    {
        private readonly JwtSettings _settings;
        private readonly byte[] _keyBytes;

        public JwtService(IOptions<JwtSettings> settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            _settings = settings.Value
                ?? throw new InvalidOperationException(
                    "تنظیمات JWT پیدا نشد.");

            ValidateSettings();

            _keyBytes = Encoding.UTF8.GetBytes(_settings.Key);
        }

        // =========================================================
        // VALIDATE SETTINGS
        // =========================================================

        private void ValidateSettings()
        {
            if (string.IsNullOrWhiteSpace(_settings.Key))
            {
                throw new InvalidOperationException(
                    "Jwt:Key تنظیم نشده است.");
            }

            // برای HS256 حداقل 32 بایت
            if (Encoding.UTF8.GetByteCount(_settings.Key) < 32)
            {
                throw new InvalidOperationException(
                    "Jwt:Key باید حداقل 32 بایت باشد.");
            }

            if (string.IsNullOrWhiteSpace(_settings.Issuer))
            {
                throw new InvalidOperationException(
                    "Jwt:Issuer تنظیم نشده است.");
            }

            if (string.IsNullOrWhiteSpace(_settings.Audience))
            {
                throw new InvalidOperationException(
                    "Jwt:Audience تنظیم نشده است.");
            }

            if (_settings.AccessTokenMinutes <= 0)
            {
                throw new InvalidOperationException(
                    "Jwt:AccessTokenMinutes باید بیشتر از صفر باشد.");
            }

            // Access Token طولانی مدت خطرناک است.
            if (_settings.AccessTokenMinutes > 60)
            {
                throw new InvalidOperationException(
                    "Access Token نباید بیشتر از 60 دقیقه باشد.");
            }

            if (_settings.RefreshTokenDays <= 0)
            {
                throw new InvalidOperationException(
                    "Jwt:RefreshTokenDays باید بیشتر از صفر باشد.");
            }

            // Refresh Token بیش از 30 روز نباشد.
            if (_settings.RefreshTokenDays > 30)
            {
                throw new InvalidOperationException(
                    "Refresh Token نباید بیشتر از 30 روز اعتبار داشته باشد.");
            }
        }

        // =========================================================
        // CREATE ACCESS TOKEN
        // =========================================================

        public string CreateAccessToken(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (user.Id <= 0)
            {
                throw new InvalidOperationException(
                    "شناسه کاربر معتبر نیست.");
            }

            if (string.IsNullOrWhiteSpace(user.PhoneNumber))
            {
                throw new InvalidOperationException(
                    "شماره موبایل کاربر معتبر نیست.");
            }

            ValidateSettings();

            var securityKey =
                new SymmetricSecurityKey(_keyBytes);

            var credentials =
                new SigningCredentials(
                    securityKey,
                    SecurityAlgorithms.HmacSha256);

            DateTime issuedAt =
                DateTime.UtcNow;

            DateTime expiresAt =
                issuedAt.AddMinutes(
                    _settings.AccessTokenMinutes);

            string tokenId =
                Convert.ToHexString(
                    RandomNumberGenerator.GetBytes(16));

            var claims =
                new List<Claim>
                {
                    new Claim(
                        JwtRegisteredClaimNames.Sub,
                        user.Id.ToString()),

                    new Claim(
                        ClaimTypes.NameIdentifier,
                        user.Id.ToString()),

                    new Claim(
                        ClaimTypes.Name,
                        user.FullName?.Trim() ?? string.Empty),

                    new Claim(
                        "phone",
                        user.PhoneNumber.Trim()),

                    new Claim(
                        JwtRegisteredClaimNames.Jti,
                        tokenId)
                };

            var token =
                new JwtSecurityToken(
                    issuer: _settings.Issuer,
                    audience: _settings.Audience,
                    claims: claims,
                    notBefore: issuedAt,
                    expires: expiresAt,
                    signingCredentials: credentials);

            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }

        // =========================================================
        // CREATE REFRESH TOKEN
        // =========================================================

        public string CreateRefreshToken()
        {
            // 64 بایت = 512 بیت entropy
            byte[] bytes =
                RandomNumberGenerator.GetBytes(64);

            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", string.Empty);
        }

        // =========================================================
        // HASH REFRESH TOKEN
        // =========================================================

        public string HashRefreshToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException(
                    "Refresh Token نمی‌تواند خالی باشد.",
                    nameof(token));
            }

            byte[] tokenBytes =
                Encoding.UTF8.GetBytes(token.Trim());

            byte[] hash =
                SHA256.HashData(tokenBytes);

            return Convert.ToHexString(hash);
        }

        // =========================================================
        // CONSTANT-TIME TOKEN COMPARISON
        // =========================================================

        public bool VerifyRefreshToken(
            string refreshToken,
            string expectedHash)
        {
            if (string.IsNullOrWhiteSpace(refreshToken) ||
                string.IsNullOrWhiteSpace(expectedHash))
            {
                return false;
            }

            string actualHash =
                HashRefreshToken(refreshToken);

            byte[] actualBytes =
                Convert.FromHexString(actualHash);

            byte[] expectedBytes;

            try
            {
                expectedBytes =
                    Convert.FromHexString(expectedHash);
            }
            catch
            {
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(
                actualBytes,
                expectedBytes);
        }

        // =========================================================
        // ACCESS TOKEN EXPIRATION
        // =========================================================

        public DateTime GetAccessTokenExpiration()
        {
            ValidateSettings();

            return DateTime.UtcNow.AddMinutes(
                _settings.AccessTokenMinutes);
        }

        // =========================================================
        // ACCESS TOKEN LIFETIME
        // =========================================================

        public TimeSpan GetAccessTokenLifetime()
        {
            ValidateSettings();

            return TimeSpan.FromMinutes(
                _settings.AccessTokenMinutes);
        }

        // =========================================================
        // REFRESH TOKEN EXPIRATION
        // =========================================================

        public DateTime GetRefreshTokenExpiration()
        {
            ValidateSettings();

            return DateTime.UtcNow.AddDays(
                _settings.RefreshTokenDays);
        }

        // =========================================================
        // REFRESH TOKEN LIFETIME
        // =========================================================

        public TimeSpan GetRefreshTokenLifetime()
        {
            ValidateSettings();

            return TimeSpan.FromDays(
                _settings.RefreshTokenDays);
        }
    }
}