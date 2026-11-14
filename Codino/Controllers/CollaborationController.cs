
using Microsoft.AspNetCore.Mvc;
using Codino.Services;

namespace Codino.Controllers
{
    public class CollaborationController : Controller
    {
        private readonly ILogger<CollaborationController> _logger;
        private readonly ApiService _apiService;

        public CollaborationController(
            ILogger<CollaborationController> logger,
            ApiService apiService)
        {
            _logger = logger;
            _apiService = apiService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // دریافت درخواست از فرم و ارسال آن به API
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(
            [FromBody] CollaborationRequestViewModel request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "اطلاعات درخواست معتبر نیست."
                });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "لطفاً اطلاعات فرم را بررسی کنید."
                });
            }

            try
            {
                var result = await _apiService.SubmitCollaborationRequestAsync(
                    request.Specialty,
                    request.Experience,
                    request.Skills,
                    request.PortfolioUrl,
                    request.Description,
                    cancellationToken);

                return Json(new
                {
                    success = result.Success,
                    message = result.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting collaboration request.");

                return StatusCode(500, new
                {
                    success = false,
                    message = "خطایی هنگام ثبت درخواست همکاری رخ داد."
                });
            }
        }
    }

    public class CollaborationRequestViewModel
    {
        public string Specialty { get; set; } = string.Empty;

        public string Experience { get; set; } = string.Empty;

        public string Skills { get; set; } = string.Empty;

        public string? PortfolioUrl { get; set; }

        public string Description { get; set; } = string.Empty;
    }
}
