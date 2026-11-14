using StackExchange.Redis;

namespace Codino.Services
{
    public class RedisRateLimitService
    {
        // =========================================================
        // REDIS CONNECTION
        // =========================================================

        private readonly IConnectionMultiplexer _redis;


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public RedisRateLimitService(
            IConnectionMultiplexer redis)
        {
            _redis =
                redis ??
                throw new ArgumentNullException(
                    nameof(redis));
        }


        // =========================================================
        // CHECK RATE LIMIT
        // =========================================================
        //
        // مثال:
        //
        // maxRequests = 5
        // window = 1 minute
        //
        // نتیجه:
        //
        // Request 1 -> Allowed
        // Request 2 -> Allowed
        // Request 3 -> Allowed
        // Request 4 -> Allowed
        // Request 5 -> Allowed
        // Request 6 -> Blocked
        //
        // =========================================================

        public async Task<bool> IsAllowedAsync(
            string key,
            int maxRequests,
            TimeSpan window)
        {
            // -----------------------------------------------------
            // VALIDATION
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException(
                    "Redis rate limit key cannot be empty.",
                    nameof(key));
            }


            if (maxRequests <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxRequests),
                    "Maximum requests must be greater than zero.");
            }


            if (window <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(window),
                    "Rate limit window must be greater than zero.");
            }


            // -----------------------------------------------------
            // REDIS CONNECTION CHECK
            // -----------------------------------------------------

            try
            {
                if (!_redis.IsConnected)
                {
                    Console.WriteLine(
                        "[REDIS RATE LIMIT] Redis is not connected.");

                    // -------------------------------------------------
                    // FAIL CLOSED
                    // -------------------------------------------------
                    //
                    // اگر Redis در دسترس نباشد،
                    // درخواست را مجاز نمی‌کنیم.
                    //
                    // این کار برای Login / Register امن‌تر است.
                    //
                    // -------------------------------------------------

                    return false;
                }


                // -----------------------------------------------------
                // GET DATABASE
                // -----------------------------------------------------

                IDatabase database =
                    _redis.GetDatabase();


                // -----------------------------------------------------
                // INCREMENT COUNTER
                // -----------------------------------------------------

                long currentCount =
                    await database.StringIncrementAsync(
                        key);


                // -----------------------------------------------------
                // CHECK TTL
                // -----------------------------------------------------
                //
                // اگر کلید تازه ساخته شده باشد،
                // TTL تنظیم می‌شود.
                //
                // همچنین اگر به هر دلیل TTL وجود نداشته باشد،
                // دوباره TTL تنظیم می‌کنیم.
                //
                // -----------------------------------------------------

                if (currentCount == 1)
                {
                    await database.KeyExpireAsync(
                        key,
                        window);
                }
                else
                {
                    TimeSpan? currentTtl =
                        await database.KeyTimeToLiveAsync(
                            key);

                    if (!currentTtl.HasValue)
                    {
                        await database.KeyExpireAsync(
                            key,
                            window);
                    }
                }


                // -----------------------------------------------------
                // CHECK LIMIT
                // -----------------------------------------------------

                bool allowed =
                    currentCount <= maxRequests;


                // -----------------------------------------------------
                // LOG
                // -----------------------------------------------------
                //
                // خود key را لاگ نمی‌کنیم.
                //
                // چون ممکن است شامل:
                // IP
                // UserId
                // Phone
                // یا اطلاعات دیگر باشد.
                //
                // -----------------------------------------------------

                Console.WriteLine(
                    "[REDIS RATE LIMIT] " +
                    $"Count: {currentCount} | " +
                    $"Max: {maxRequests} | " +
                    $"Window: {window} | " +
                    $"Allowed: {allowed}");


                return allowed;
            }
            catch (RedisConnectionException ex)
            {
                Console.WriteLine(
                    "[REDIS RATE LIMIT] " +
                    "Redis connection error.");

                Console.WriteLine(
                    ex.Message);


                // -----------------------------------------------------
                // FAIL CLOSED
                // -----------------------------------------------------

                return false;
            }
            catch (RedisTimeoutException ex)
            {
                Console.WriteLine(
                    "[REDIS RATE LIMIT] " +
                    "Redis timeout.");

                Console.WriteLine(
                    ex.Message);


                // -----------------------------------------------------
                // FAIL CLOSED
                // -----------------------------------------------------

                return false;
            }
            catch (RedisException ex)
            {
                Console.WriteLine(
                    "[REDIS RATE LIMIT] " +
                    "Redis error.");

                Console.WriteLine(
                    ex.Message);


                // -----------------------------------------------------
                // FAIL CLOSED
                // -----------------------------------------------------

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "[REDIS RATE LIMIT] " +
                    "Unexpected error.");

                Console.WriteLine(
                    ex.Message);


                // -----------------------------------------------------
                // FAIL CLOSED
                // -----------------------------------------------------

                return false;
            }
        }


        // =========================================================
        // RESET RATE LIMIT
        // =========================================================
        //
        // حذف کامل Counter.
        //
        // مثال:
        //
        // await ResetAsync(
        //     "login-rate:192.168.1.10");
        //
        // =========================================================

        public async Task<bool> ResetAsync(
            string key)
        {
            // -----------------------------------------------------
            // VALIDATION
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }


            try
            {
                // -------------------------------------------------
                // REDIS CONNECTION CHECK
                // -------------------------------------------------

                if (!_redis.IsConnected)
                {
                    Console.WriteLine(
                        "[REDIS RATE LIMIT RESET] " +
                        "Redis is not connected.");

                    return false;
                }


                // -------------------------------------------------
                // GET DATABASE
                // -------------------------------------------------

                IDatabase database =
                    _redis.GetDatabase();


                // -------------------------------------------------
                // DELETE KEY
                // -------------------------------------------------

                bool deleted =
                    await database.KeyDeleteAsync(
                        key);


                // -------------------------------------------------
                // LOG
                // -------------------------------------------------

                Console.WriteLine(
                    "[REDIS RATE LIMIT RESET] " +
                    $"Deleted: {deleted}");


                return deleted;
            }
            catch (RedisConnectionException ex)
            {
                Console.WriteLine(
                    "[REDIS RATE LIMIT RESET] " +
                    "Redis connection error.");

                Console.WriteLine(
                    ex.Message);

                return false;
            }
            catch (RedisTimeoutException ex)
            {
                Console.WriteLine(
                    "[REDIS RATE LIMIT RESET] " +
                    "Redis timeout.");

                Console.WriteLine(
                    ex.Message);

                return false;
            }
            catch (RedisException ex)
            {
                Console.WriteLine(
                    "[REDIS RATE LIMIT RESET] " +
                    "Redis error.");

                Console.WriteLine(
                    ex.Message);

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "[REDIS RATE LIMIT RESET] " +
                    "Unexpected error.");

                Console.WriteLine(
                    ex.Message);

                return false;
            }
        }


        // =========================================================
        // GET CURRENT COUNT
        // =========================================================
        //
        // تعداد درخواست‌های فعلی یک Key را برمی‌گرداند.
        //
        // =========================================================

        public async Task<long> GetCurrentCountAsync(
            string key)
        {
            // -----------------------------------------------------
            // VALIDATION
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(key))
            {
                return 0;
            }


            try
            {
                // -------------------------------------------------
                // REDIS CONNECTION CHECK
                // -------------------------------------------------

                if (!_redis.IsConnected)
                {
                    return 0;
                }


                // -------------------------------------------------
                // GET DATABASE
                // -------------------------------------------------

                IDatabase database =
                    _redis.GetDatabase();


                // -------------------------------------------------
                // GET VALUE
                // -------------------------------------------------

                RedisValue value =
                    await database.StringGetAsync(
                        key);


                // -------------------------------------------------
                // KEY DOES NOT EXIST
                // -------------------------------------------------

                if (!value.HasValue)
                {
                    return 0;
                }


                // -------------------------------------------------
                // PARSE VALUE
                // -------------------------------------------------

                if (long.TryParse(
                        value.ToString(),
                        out long count))
                {
                    return count;
                }


                return 0;
            }
            catch (RedisConnectionException ex)
            {
                Console.WriteLine(
                    "[REDIS RATE LIMIT COUNT] " +
                    "Redis connection error.");

                Console.WriteLine(
                    ex.Message);

                return 0;
            }
            catch (RedisTimeoutException ex)
            {
                Console.WriteLine(
                    "[REDIS RATE LIMIT COUNT] " +
                    "Redis timeout.");

                Console.WriteLine(
                    ex.Message);

                return 0;
            }
            catch (RedisException ex)
            {
                Console.WriteLine(
                    "[REDIS RATE LIMIT COUNT] " +
                    "Redis error.");

                Console.WriteLine(
                    ex.Message);

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "[REDIS RATE LIMIT COUNT] " +
                    "Unexpected error.");

                Console.WriteLine(
                    ex.Message);

                return 0;
            }
        }


        // =========================================================
        // GET REMAINING TTL
        // =========================================================
        //
        // مدت زمان باقی‌مانده Counter را برمی‌گرداند.
        //
        // این متد برای نمایش اطلاعات دقیق‌تر در آینده
        // می‌تواند استفاده شود.
        //
        // =========================================================

        public async Task<TimeSpan?> GetRemainingTimeAsync(
            string key)
        {
            // -----------------------------------------------------
            // VALIDATION
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }


            try
            {
                // -------------------------------------------------
                // REDIS CONNECTION CHECK
                // -------------------------------------------------

                if (!_redis.IsConnected)
                {
                    return null;
                }


                // -------------------------------------------------
                // GET DATABASE
                // -------------------------------------------------

                IDatabase database =
                    _redis.GetDatabase();


                // -------------------------------------------------
                // GET TTL
                // -------------------------------------------------

                return await database.KeyTimeToLiveAsync(
                    key);
            }
            catch (RedisConnectionException ex)
            {
                Console.WriteLine(
                    "[REDIS RATE LIMIT TTL] " +
                    "Redis connection error.");

                Console.WriteLine(
                    ex.Message);

                return null;
            }
            catch (RedisTimeoutException ex)
            {
                Console.WriteLine(
                    "[REDIS RATE LIMIT TTL] " +
                    "Redis timeout.");

                Console.WriteLine(
                    ex.Message);

                return null;
            }
            catch (RedisException ex)
            {
                Console.WriteLine(
                    "[REDIS RATE LIMIT TTL] " +
                    "Redis error.");

                Console.WriteLine(
                    ex.Message);

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "[REDIS RATE LIMIT TTL] " +
                    "Unexpected error.");

                Console.WriteLine(
                    ex.Message);

                return null;
            }
        }
    }
}