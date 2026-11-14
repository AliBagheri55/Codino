using Codino.Api.Models;
using Codino.Api.Settings;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

using System.Text.Json;

namespace Codino.Api.Services
{
    public sealed class RefreshTokenService
    {
        private readonly IDistributedCache _cache;
        private readonly JwtService _jwtService;
        private readonly JwtSettings _settings;

        private readonly JsonSerializerOptions _jsonOptions =
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

        public RefreshTokenService(
            IDistributedCache cache,
            JwtService jwtService,
            IOptions<JwtSettings> settings)
        {
            _cache = cache
                ?? throw new ArgumentNullException(nameof(cache));

            _jwtService = jwtService
                ?? throw new ArgumentNullException(nameof(jwtService));

            _settings = settings?.Value
                ?? throw new ArgumentNullException(nameof(settings));

            ValidateSettings();
        }

        // =========================================================
        // VALIDATE SETTINGS
        // =========================================================

        private void ValidateSettings()
        {
            if (_settings.RefreshTokenDays <= 0)
            {
                throw new InvalidOperationException(
                    "Jwt:RefreshTokenDays باید بیشتر از صفر باشد.");
            }

            if (_settings.RefreshTokenDays > 30)
            {
                throw new InvalidOperationException(
                    "Jwt:RefreshTokenDays نباید بیشتر از 30 روز باشد.");
            }
        }

        // =========================================================
        // SAVE REFRESH TOKEN
        // =========================================================

        public async Task SaveAsync(
            int userId,
            string refreshToken,
            CancellationToken cancellationToken = default)
        {
            if (userId <= 0)
            {
                throw new ArgumentException(
                    "شناسه کاربر معتبر نیست.",
                    nameof(userId));
            }

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                throw new ArgumentException(
                    "Refresh Token نمی‌تواند خالی باشد.",
                    nameof(refreshToken));
            }

            ValidateSettings();

            string token =
                refreshToken.Trim();

            string hash =
                _jwtService.HashRefreshToken(token);

            DateTime createdAt =
                DateTime.UtcNow;

            DateTime expiresAt =
                _jwtService.GetRefreshTokenExpiration();

            var data =
                new RefreshTokenRedis
                {
                    UserId = userId,

                    TokenHash = hash,

                    CreatedAt = createdAt,

                    ExpiresAt = expiresAt,

                    Revoked = false
                };

            string json =
                JsonSerializer.Serialize(
                    data,
                    _jsonOptions);

            string key =
                BuildRedisKey(hash);

            TimeSpan ttl =
                expiresAt - DateTime.UtcNow;

            if (ttl <= TimeSpan.Zero)
            {
                throw new InvalidOperationException(
                    "Refresh Token زمان اعتبار معتبری ندارد.");
            }

            await _cache.SetStringAsync(
                key,
                json,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = ttl
                },
                cancellationToken);
        }

        // =========================================================
        // GET REFRESH TOKEN
        // =========================================================

        public async Task<RefreshTokenRedis?> GetAsync(
            string refreshToken,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return null;

            string token =
                refreshToken.Trim();

            string hash =
                _jwtService.HashRefreshToken(token);

            string key =
                BuildRedisKey(hash);

            string? json =
                await _cache.GetStringAsync(
                    key,
                    cancellationToken);

            if (string.IsNullOrWhiteSpace(json))
                return null;

            RefreshTokenRedis? data;

            try
            {
                data =
                    JsonSerializer.Deserialize<RefreshTokenRedis>(
                        json,
                        _jsonOptions);
            }
            catch (JsonException)
            {
                await _cache.RemoveAsync(
                    key,
                    cancellationToken);

                return null;
            }

            if (data == null)
            {
                await _cache.RemoveAsync(
                    key,
                    cancellationToken);

                return null;
            }

            // =====================================================
            // USER ID
            // =====================================================

            if (data.UserId <= 0)
            {
                await _cache.RemoveAsync(
                    key,
                    cancellationToken);

                return null;
            }

            // =====================================================
            // HASH MATCH
            // =====================================================

            if (!_jwtService.VerifyRefreshToken(
                    token,
                    data.TokenHash))
            {
                return null;
            }

            // =====================================================
            // REVOKED
            // =====================================================

            if (data.Revoked)
            {
                await _cache.RemoveAsync(
                    key,
                    cancellationToken);

                return null;
            }

            // =====================================================
            // EXPIRATION
            // =====================================================

            if (DateTime.UtcNow >= data.ExpiresAt)
            {
                await _cache.RemoveAsync(
                    key,
                    cancellationToken);

                return null;
            }

            return data;
        }

        // =========================================================
        // REVOKE
        // =========================================================

        public async Task RevokeAsync(
            string refreshToken,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return;

            string hash =
                _jwtService.HashRefreshToken(
                    refreshToken.Trim());

            string key =
                BuildRedisKey(hash);

            await _cache.RemoveAsync(
                key,
                cancellationToken);
        }

        // =========================================================
        // ROTATE REFRESH TOKEN
        // =========================================================

        public async Task<string> RotateAsync(
            RefreshTokenRedis oldToken,
            CancellationToken cancellationToken = default)
        {
            if (oldToken == null)
            {
                throw new ArgumentNullException(
                    nameof(oldToken));
            }

            if (oldToken.UserId <= 0)
            {
                throw new InvalidOperationException(
                    "کاربر Refresh Token معتبر نیست.");
            }

            // ساخت Refresh Token جدید
            string newRefreshToken =
                _jwtService.CreateRefreshToken();

            // ذخیره Token جدید
            await SaveAsync(
                oldToken.UserId,
                newRefreshToken,
                cancellationToken);

            return newRefreshToken;
        }

        // =========================================================
        // BUILD REDIS KEY
        // =========================================================

        private static string BuildRedisKey(
            string hash)
        {
            if (string.IsNullOrWhiteSpace(hash))
            {
                throw new ArgumentException(
                    "Hash معتبر نیست.",
                    nameof(hash));
            }

            return $"refresh-token:{hash}";
        }
    }
}