using System.Text.Json;
using Codino.Api.Data;
using Codino.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Codino.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CoursesController : ControllerBase
    {
        private readonly CodinoDbContext _context;
        private readonly IDistributedCache _cache;

        private const string CacheKey = "courses:all";

        public CoursesController(
            CodinoDbContext context,
            IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
        }
[HttpGet("{slug}")]
public async Task<IActionResult> GetCourseBySlug(string slug)
        {
            string cacheKey = $"course:{slug}";

            string? cachedCourse =
                await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedCourse))
            {
                CourseResponse? courseFromCache =
                    JsonSerializer.Deserialize<CourseResponse>(
                        cachedCourse);

                if (courseFromCache != null)
                {
                    return Ok(courseFromCache);
                }
            }

            CourseResponse? course =
                await _context.Courses
                    .AsNoTracking()
                    .Where(x =>
                        x.IsActive &&
                        x.Slug == slug)
                    .Select(x => new CourseResponse
                    {
                        Id = x.Id,
                        Title = x.Title,
                        Description = x.Description,
                        Image = x.Image,
                        Price = x.Price,
                        Chapters = x.Chapters,
                        Sessions = x.Sessions,
                        Slug = x.Slug
                    })
                    .FirstOrDefaultAsync();

            if (course == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "دوره مورد نظر پیدا نشد."
                });
            }

            string json =
                JsonSerializer.Serialize(course);

            DistributedCacheEntryOptions options =
                new()
                {
                    AbsoluteExpirationRelativeToNow =
                        TimeSpan.FromMinutes(10)
                };

            await _cache.SetStringAsync(
                cacheKey,
                json,
                options);

            return Ok(course);
        }

        [HttpGet]
        public async Task<IActionResult> GetCourses()
        {
            // =========================
            // 1. بررسی Redis
            // =========================

            string? cachedCourses =
                await _cache.GetStringAsync(CacheKey);

            if (!string.IsNullOrEmpty(cachedCourses))
            {
                List<CourseResponse>? coursesFromCache =
                    JsonSerializer.Deserialize<List<CourseResponse>>(
                        cachedCourses);

                if (coursesFromCache != null)
                {
                    return Ok(coursesFromCache);
                }
            }

            // =========================
            // 2. دریافت از SQL Server
            // =========================

            List<CourseResponse> courses =
                await _context.Courses
                    .AsNoTracking()
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Id)
                    .Select(x => new CourseResponse
                    {
                        Id = x.Id,
                        Title = x.Title,
                        Description = x.Description,
                        Image = x.Image,
                        Price = x.Price,
                        Chapters = x.Chapters,
                        Sessions = x.Sessions
                    })
                    .ToListAsync();

            // =========================
            // 3. ذخیره در Redis
            // =========================

            string json =
                JsonSerializer.Serialize(courses);

            DistributedCacheEntryOptions options =
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow =
                        TimeSpan.FromMinutes(10)
                };

            await _cache.SetStringAsync(
                CacheKey,
                json,
                options);

            // =========================
            // 4. ارسال نتیجه
            // =========================

            return Ok(courses);
        }
    }
}