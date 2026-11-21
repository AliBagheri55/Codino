
using System.Security.Claims;
using Codino.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Codino.Api.Controllers
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Roles = "Admin")]
    public class AdminUsersController : ControllerBase
    {
        private readonly CodinoDbContext _context;

        public AdminUsersController(CodinoDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // GET: api/admin/users
        // دریافت فهرست کاربران همراه با جست‌وجو و صفحه‌بندی
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> GetUsers(
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            // اعتبارسنجی صفحه‌بندی
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _context.Users.AsNoTracking();

            // جست‌وجو بر اساس نام یا شماره موبایل
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(x =>
                    x.FullName.Contains(search) ||
                    x.PhoneNumber.Contains(search));
            }

            // تعداد کل کاربران مطابق فیلتر
            int totalCount = await query.CountAsync();

            // دریافت اطلاعات صفحه موردنظر
            var users = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new
                {
                    x.Id,
                    x.FullName,
                    x.PhoneNumber,
                    x.Role,
                    x.CreatedAt,
                    x.IsPhoneVerified
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = users,
                pagination = new
                {
                    page,
                    pageSize,
                    totalCount,
                    totalPages = (int)Math.Ceiling(
                        totalCount / (double)pageSize)
                }
            });
        }

        // =====================================================
        // GET: api/admin/users/5
        // مشاهده جزئیات یک کاربر
        // =====================================================

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetUser(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "شناسه کاربر معتبر نیست."
                });
            }

            var user = await _context.Users
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new
                {
                    x.Id,
                    x.FullName,
                    x.PhoneNumber,
                    x.Role,
                    x.CreatedAt,
                    x.IsPhoneVerified
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "کاربر موردنظر پیدا نشد."
                });
            }

            return Ok(new
            {
                success = true,
                data = user
            });
        }

        // =====================================================
        // DELETE: api/admin/users/5
        // حذف کاربر
        // =====================================================

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "شناسه کاربر معتبر نیست."
                });
            }

            // دریافت شناسه مدیر واردشده
            string? currentUserId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            // جلوگیری از حذف حساب مدیر فعلی
            if (int.TryParse(currentUserId, out int adminId) &&
                adminId == id)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "امکان حذف حساب کاربری خودتان وجود ندارد."
                });
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == id);

            if (user == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "کاربر موردنظر پیدا نشد."
                });
            }

            try
            {
                _context.Users.Remove(user);

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "کاربر با موفقیت حذف شد."
                });
            }
            catch (DbUpdateException)
            {
                // ممکن است کاربر به اطلاعات دیگری مانند
                // نظرات یا درخواست‌های پروژه متصل باشد.
                return Conflict(new
                {
                    success = false,
                    message =
                        "این کاربر به اطلاعات دیگری در سایت متصل است و فعلاً قابل حذف نیست."
                });
            }
        }
    }
}
