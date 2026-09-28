using StackExchange.Redis;

namespace Codino.Api.Services
{
    public class RedisLoginAttemptService
    {
        private readonly IConnectionMultiplexer _redis;

        public RedisLoginAttemptService(
            IConnectionMultiplexer redis)
        {
            _redis = redis;
        }

        public async Task<TimeSpan?> GetLockTimeAsync(
            string ipAddress,
            string phoneNumber)
        {
            IDatabase database =
                _redis.GetDatabase();

            string redisKey =
                $"login-lock:{ipAddress}:{phoneNumber}";

            if (!await database.KeyExistsAsync(redisKey))
            {
                return null;
            }

            return await database.KeyTimeToLiveAsync(
                redisKey);
        }

        public async Task<int> RegisterFailedAttemptAsync(
            string ipAddress,
            string phoneNumber)
        {
            IDatabase database =
                _redis.GetDatabase();

            string redisKey =
                $"login-attempts:{ipAddress}:{phoneNumber}";

            long count =
                (long)await database.ScriptEvaluateAsync(
                    """
                    local count = redis.call('INCR', KEYS[1])

                    if count == 1 then
                        redis.call('EXPIRE', KEYS[1], 900)
                    end

                    return count
                    """,
                    new RedisKey[]
                    {
                        redisKey
                    },
                    Array.Empty<RedisValue>());

            if (count >= 3)
            {
                string lockKey =
                    $"login-lock:{ipAddress}:{phoneNumber}";

                await database.StringSetAsync(
                    lockKey,
                    "locked",
                    TimeSpan.FromMinutes(3));

                await database.KeyDeleteAsync(
                    redisKey);

                return 3;
            }

            return (int)count;
        }

        public async Task ClearAttemptsAsync(
            string ipAddress,
            string phoneNumber)
        {
            IDatabase database =
                _redis.GetDatabase();

            string redisKey =
                $"login-attempts:{ipAddress}:{phoneNumber}";

            await database.KeyDeleteAsync(
                redisKey);
        }
    }
}