using Codino.Api.Data;
using Codino.Api.DTOs;
using Codino.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Codino.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommentsController : ControllerBase
    {
        private readonly CodinoDbContext _context;

        public CommentsController(
            CodinoDbContext context)
        {
            _context = context;
        }


        // =========================
        // GET COMMENTS
        // =========================

        [HttpGet]
        public async Task<IActionResult> GetComments()
        {
            List<CommentResponse> comments =
                await _context.Comments
                    .AsNoTracking()
                    .Where(x => x.IsApproved)
                    .OrderByDescending(x => x.CreatedAt)
                    .Select(x => new CommentResponse
                    {
                        Id = x.Id,

                        UserName =
                            x.User != null
                                ? x.User.FullName
                                : "کاربر",

                        Text = x.Text,

                        Rating = x.Rating,

                        CreatedAt = x.CreatedAt
                    })
                    .ToListAsync();


            return Ok(comments);
        }


        // =========================
        // CREATE COMMENT
        // =========================

        [HttpPost]
        public async Task<IActionResult> CreateComment(
            [FromBody] CreateCommentRequest request)
        {
            if (request == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "اطلاعات نظر ارسال نشده است."
                });
            }


            if (request.UserId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "کاربر معتبر نیست."
                });
            }


            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "متن نظر را وارد کنید."
                });
            }


            string text =
                request.Text.Trim();


            if (text.Length > 1000)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "متن نظر نمی‌تواند بیشتر از ۱۰۰۰ کاراکتر باشد."
                });
            }


            if (request.Rating < 1 ||
                request.Rating > 5)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "امتیاز باید بین ۱ تا ۵ باشد."
                });
            }


            User? user =
                await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.Id == request.UserId);


            if (user == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "کاربر پیدا نشد."
                });
            }


            if (!user.IsPhoneVerified)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "شماره موبایل کاربر تأیید نشده است."
                });
            }


            Comment comment =
                new Comment
                {
                    UserId = user.Id,

                    Text = text,

                    Rating = request.Rating,

                    CreatedAt = DateTime.UtcNow,

                    IsApproved = true
                };


            _context.Comments.Add(comment);

            await _context.SaveChangesAsync();


            return Ok(new
            {
                success = true,

                message =
                    "نظر شما با موفقیت ثبت شد.",

                commentId = comment.Id
            });
        }
    }
}