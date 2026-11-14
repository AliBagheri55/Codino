using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Codino.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetProfile()
        {
            string? userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            string? fullName =
                User.FindFirstValue(
                    ClaimTypes.Name);

            string? phone =
                User.FindFirstValue("phone");

            return Ok(
                new
                {
                    Success = true,

                    UserId = userId,

                    FullName = fullName,

                    PhoneNumber = phone,

                    Message =
                        "این اطلاعات فقط برای کاربر احراز هویت شده قابل دسترسی است."
                });
        }
    }
}