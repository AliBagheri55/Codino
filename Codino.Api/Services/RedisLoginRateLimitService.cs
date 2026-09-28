using StackExchange.Redis;

namespace Codino.Api.Services
{
    public class RedisLoginRateLimitService
    {
        private readonly IConnectionMultiplexer _redis;

        public RedisLoginRateLimitService(
            IConnectionMultiplexer redis)
        {
            _redis = redis;
        }

        public async Task<bool> IsAllowedAsync(
            string ipAddress)
        {
            IDatabase database =
                _redis.GetDatabase();

            string redisKey =
                $"login-rate-limit:{ipAddress}";

            long count =
                (long)await database.ScriptEvaluateAsync(
                    """
                    local count = redis.call('INCR', KEYS[1])

                    if count == 1 then
                        redis.call('EXPIRE', KEYS[1], ARGV[1])
                    end

                    return count
                    """,
                    new RedisKey[]
                    {
                        redisKey
                    },
                    new RedisValue[]
                    {
                        120
                    });

            return count <= 1;
        }
    }
}
