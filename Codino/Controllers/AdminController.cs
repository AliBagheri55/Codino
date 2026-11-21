
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codino.Controllers
{
    // فقط مدیر سایت اجازه ورود به پنل مدیریت را دارد.
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        // صفحه اصلی پنل مدیریت
        [HttpGet]
        public IActionResult Index()
        {
            ViewData["Title"] = "داشبورد مدیریت";
            ViewData["PageTitle"] = "داشبورد مدیریت";

            return View();
        }
    }
}
