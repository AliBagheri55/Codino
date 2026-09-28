using Codino.Models;
using Codino.Models.Api;
using Codino.Services;
using Microsoft.AspNetCore.Mvc;

namespace Codino.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApiService _apiService;


        public HomeController(
            ApiService apiService)
        {
            _apiService =
                apiService;
        }


        // =========================
        // HOME
        // =========================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            List<CommentResponse> comments =
                await _apiService.GetCommentsAsync();

            List<CourseResponse> courses =
                await _apiService.GetCoursesAsync();


            HomeViewModel model =
                new()
                {
                    Comments = comments,

                    Courses = courses
                };


            return View(model);
        }


        // =========================
        // CREATE COMMENT
        // =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateComment(
            int rating,
            string text)
        {
            string? userIdString =
                HttpContext.Session.GetString(
                    "UserId");


            if (string.IsNullOrWhiteSpace(
                    userIdString))
            {
                TempData["CommentError"] =
                    "برای ثبت نظر ابتدا وارد حساب کاربری شوید.";

                return RedirectToAction(
                    nameof(Index));
            }


            if (!int.TryParse(
                    userIdString,
                    out int userId))
            {
                TempData["CommentError"] =
                    "شناسه کاربر معتبر نیست.";

                return RedirectToAction(
                    nameof(Index));
            }


            if (string.IsNullOrWhiteSpace(text))
            {
                TempData["CommentError"] =
                    "متن نظر را وارد کنید.";

                return RedirectToAction(
                    nameof(Index));
            }


            text =
                text.Trim();


            if (text.Length > 1000)
            {
                TempData["CommentError"] =
                    "متن نظر نمی‌تواند بیشتر از ۱۰۰۰ کاراکتر باشد.";

                return RedirectToAction(
                    nameof(Index));
            }


            if (rating < 1 || rating > 5)
            {
                TempData["CommentError"] =
                    "امتیاز باید بین ۱ تا ۵ باشد.";

                return RedirectToAction(
                    nameof(Index));
            }


            CreateCommentRequest request =
                new()
                {
                    UserId = userId,

                    Text = text,

                    Rating = rating
                };


            ApiResponse? response =
                await _apiService.CreateCommentAsync(
                    request);


            if (response == null)
            {
                TempData["CommentError"] =
                    "ارتباط با سرور برقرار نشد.";

                return RedirectToAction(
                    nameof(Index));
            }


            if (!response.Success)
            {
                TempData["CommentError"] =
                    string.IsNullOrWhiteSpace(
                        response.Message)
                            ? "ثبت نظر انجام نشد."
                            : response.Message;

                return RedirectToAction(
                    nameof(Index));
            }


            TempData["CommentSuccess"] =
                "نظر شما با موفقیت ثبت شد.";


            return RedirectToAction(
                nameof(Index));
        }
    }
}