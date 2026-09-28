using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;

namespace Codino.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RedisTestController : ControllerBase
{
    private readonly IDistributedCache _cache;

    public RedisTestController(IDistributedCache cache)
    {
        _cache = cache;
    }

    // =========================
    // SET + GET TEST
    // =========================

    [HttpGet("test")]
    public async Task<IActionResult> TestRedis()
    {
        const string key = "codino:redis:test";
        const string value = "Redis Connected Successfully!";

        // ذخیره اطلاعات در Redis
        await _cache.SetStringAsync(
            key,
            value,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow =
                    TimeSpan.FromMinutes(10)
            });

        // خواندن اطلاعات از Redis
        var result = await _cache.GetStringAsync(key);

        return Ok(new
        {
            success = result == value,
            key = key,
            value = result
        });
    }
}