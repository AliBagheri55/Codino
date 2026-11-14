
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Codino.Models;
using Codino.Models.Api;
using Codino.Services;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Codino.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApiService _apiService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            ApiService apiService,
            ILogger<HomeController> logger)
        {
            _apiService = apiService
                ?? throw new ArgumentNullException(nameof(apiService));

            _logger = logger
                ?? throw new ArgumentNullException(nameof(logger));
        }

        // =========================================================
        // HOME PAGE
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index(
            CancellationToken cancellationToken)
        {
            List<CommentResponse> comments = new();

            // LOAD COMMENTS
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                List<CommentResponse>? result =
                    await _apiService.GetCommentsAsync();

                cancellationToken.ThrowIfCancellationRequested();

                comments = (result ?? new List<CommentResponse>())
                    .Where(comment => comment != null)
                    .OrderByDescending(comment => comment.CreatedAt)
                    .Take(2)
                    .ToList();

                _logger.LogInformation(
                    "Comments loaded from API. Count: {Count}",
                    comments.Count);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Timeout while loading comments.");

                TempData["HomeCommentsError"] =
                    "دریافت نظرات بیش از حد طول کشید.";
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "HTTP error while loading comments.");

                TempData["HomeCommentsError"] =
                    "ارتباط با سرور نظرات برقرار نشد.";
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to load comments from API.");

                TempData["HomeCommentsError"] =
                    "دریافت نظرات با مشکل مواجه شد.";
            }

            // LOAD COURSES
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Timeout while loading courses.");

                TempData["HomeCoursesError"] =
                    "دریافت دوره‌ها بیش از حد طول کشید.";
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "HTTP error while loading courses.");

                TempData["HomeCoursesError"] =
                    "ارتباط با سرور دوره‌ها برقرار نشد.";
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to load courses from API.");

                TempData["HomeCoursesError"] =
                    "دریافت دوره‌ها با مشکل مواجه شد.";
            }

            // HOME VIEW MODEL
            HomeViewModel model = new()
            {
                Comments = comments,
            };

            // اطلاعات دوره متنی C#
            ViewBag.CSharpCourseSlug = "csharp";
            ViewBag.CSharpCourseHasVideos = false;
            ViewBag.CSharpCourseHasPdf = false;
            ViewBag.CSharpCourseHasText = true;

            ViewBag.CSharpCourseTotalStages = 20;
            ViewBag.CSharpCourseTopicsPerStage = 3;
            ViewBag.CSharpCourseQuestionsPerStage = 3;

            ViewBag.CSharpCourseTotalTopics = 60;
            ViewBag.CSharpCourseTotalQuestions = 60;
            return View(model);
        }

        // =========================================================
        // PRIVACY POLICY
        // URL: /Home/Privacy
        // VIEW: Views/Home/Privacy.cshtml
        // =========================================================

        [HttpGet]
        public IActionResult Privacy()
        {
            ViewData["Title"] = "حریم خصوصی | کدینو";

            return View();
        }

        // =========================================================
        // TERMS AND CONDITIONS
        // URL: /Home/Terms
        // VIEW: Views/Home/Terms.cshtml
        // =========================================================

        [HttpGet]
        public IActionResult Terms()
        {
            ViewData["Title"] = "قوانین و مقررات | کدینو";

            return View();
        }

        // =========================================================
        // CONTACT US
        // URL: /Home/Contact
        // VIEW: Views/Home/Contact.cshtml
        // =========================================================

        [HttpGet]
        public IActionResult Contact()
        {
            ViewData["Title"] = "تماس با ما | کدینو";

            return View();
        }

        // =========================================================
        // CREATE COMMENT
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateComment(
            int rating,
            string? text,
            CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                string? userIdString =
                    HttpContext.Session.GetString("UserId");

                if (string.IsNullOrWhiteSpace(userIdString))
                {
                    TempData["CommentError"] =
                        "برای ثبت نظر ابتدا وارد حساب کاربری شوید.";

                    return RedirectToAction(nameof(Index));
                }

                if (!int.TryParse(userIdString, out int userId) ||
                    userId <= 0)
                {
                    TempData["CommentError"] =
                        "شناسه کاربر معتبر نیست.";

                    return RedirectToAction(nameof(Index));
                }

                if (rating < 1 || rating > 5)
                {
                    TempData["CommentError"] =
                        "امتیاز باید بین ۱ تا ۵ باشد.";

                    return RedirectToAction(nameof(Index));
                }

                text = NormalizeComment(text);

                if (string.IsNullOrWhiteSpace(text))
                {
                    TempData["CommentError"] =
                        "متن نظر را وارد کنید.";

                    return RedirectToAction(nameof(Index));
                }

                if (text.Length > 500)
                {
                    TempData["CommentError"] =
                        "نظر نمی‌تواند بیشتر از ۵۰۰ کاراکتر باشد.";

                    return RedirectToAction(nameof(Index));
                }

                if (ContainsDangerousContent(text))
                {
                    TempData["CommentError"] =
                        "متن نظر شامل محتوای غیرمجاز است.";

                    return RedirectToAction(nameof(Index));
                }

                cancellationToken.ThrowIfCancellationRequested();

                CreateCommentRequest request = new()
                {
                    UserId = userId,
                    Text = text,
                    Rating = rating
                };

                ApiResponse? response =
                    await _apiService.CreateCommentAsync(request);

                cancellationToken.ThrowIfCancellationRequested();

                if (response == null)
                {
                    TempData["CommentError"] =
                        "پاسخی از سرور دریافت نشد. لطفاً دوباره تلاش کنید.";

                    return RedirectToAction(nameof(Index));
                }

                if (!response.Success)
                {
                    TempData["CommentError"] =
                        string.IsNullOrWhiteSpace(response.Message)
                            ? "ثبت نظر انجام نشد."
                            : response.Message;

                    return RedirectToAction(nameof(Index));
                }

                TempData["CommentSuccess"] =
                    "نظر شما با موفقیت ثبت شد.";

                return RedirectToAction(nameof(Index));
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation(
                    "Create comment request was canceled.");

                throw;
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Timeout while creating a comment.");

                TempData["CommentError"] =
                    "زمان ثبت نظر به پایان رسید. لطفاً دوباره تلاش کنید.";

                return RedirectToAction(nameof(Index));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "HTTP error while creating a comment.");

                TempData["CommentError"] =
                    "ارتباط با سرور برقرار نشد. لطفاً بعداً دوباره تلاش کنید.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while creating a comment.");

                TempData["CommentError"] =
                    "خطایی هنگام ثبت نظر رخ داد.";

                return RedirectToAction(nameof(Index));
            }
        }

        // =========================================================
        // NORMALIZE COMMENT
        // =========================================================

        private static string NormalizeComment(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            return Regex.Replace(
                text.Trim(),
                @"[^\S\r\n]+",
                " ");
        }

        // =========================================================
        // VALIDATE COMMENT CONTENT
        // =========================================================

        private static bool ContainsDangerousContent(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            if (Regex.IsMatch(
                    text,
                    @"<\s*/?\s*[a-zA-Z][^>]*>",
                    RegexOptions.IgnoreCase |
                    RegexOptions.CultureInvariant))
            {
                return true;
            }

            if (Regex.IsMatch(
                    text,
                    @"\b(?:javascript|data)\s*:",
                    RegexOptions.IgnoreCase |
                    RegexOptions.CultureInvariant))
            {
                return true;
            }

            if (Regex.IsMatch(
                    text,
                    @"\bon[a-zA-Z]+\s*=",
                    RegexOptions.IgnoreCase |
                    RegexOptions.CultureInvariant))
            {
                return true;
            }

            if (Regex.IsMatch(
                    text,
                    @"&lt;\s*/?\s*[a-zA-Z]",
                    RegexOptions.IgnoreCase |
                    RegexOptions.CultureInvariant))
            {
                return true;
            }

            return false;
        }
    }
}
