using Codino.Models.Api;
using Codino.Services;
using Microsoft.AspNetCore.Mvc;

namespace Codino.Controllers
{
    public class CourseController : Controller
    {
        private readonly ApiService _apiService;

        public CourseController(ApiService apiService)
        {
            _apiService = apiService;
        }

        [HttpGet]
        public async Task<IActionResult> Details(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return NotFound();
            }

            CourseResponse? course =
                await _apiService.GetCourseBySlugAsync(slug);

            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }
    }
}
