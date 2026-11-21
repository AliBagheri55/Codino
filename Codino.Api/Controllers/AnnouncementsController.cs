
using Codino.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Codino.Api.Controllers
{
    [ApiController]
    [Route("api/announcements")]
    public class AnnouncementsController : ControllerBase
    {
        private readonly CodinoDbContext _context;

        public AnnouncementsController(CodinoDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetPublished(
            CancellationToken cancellationToken)
        {
            var announcements = await _context.Announcements
                .AsNoTracking()
                .Where(x => x.IsPublished)
                .OrderByDescending(x => x.CreatedAtUtc)
                .Select(x => new
                {
                    x.Id,
                    x.Title,
                    x.Content,
                    x.AuthorLabel,
                    x.CreatedAtUtc
                })
                .ToListAsync(cancellationToken);

            return Ok(new
            {
                success = true,
                data = announcements
            });
        }
    }
}
