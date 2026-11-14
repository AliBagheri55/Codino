using System.Security.Claims;
using System.Text.RegularExpressions;

using Codino.Api.Data;
using Codino.Api.DTOs;
using Codino.Api.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Codino.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class CommentsController : ControllerBase
    {
        private readonly CodinoDbContext _context;
        private readonly ILogger<CommentsController> _logger;

        private const int MaxCommentLength = 500;

        public CommentsController(
            CodinoDbContext context,
            ILogger<CommentsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // =========================================================
        // GET COMMENTS
        // GET: api/Comments
        // =========================================================

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetComments(
            CancellationToken cancellationToken)
        {
            try
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
                                x.User != null &&
                                !string.IsNullOrWhiteSpace(
                                    x.User.FullName)
                                    ? x.User.FullName
                                    : "کاربر",

                            Text = x.Text,

                            Rating = x.Rating,

                            CreatedAt = x.CreatedAt
                        })
                        .ToListAsync(cancellationToken);

                _logger.LogInformation(
                    "Comments loaded successfully. Count={Count}",
                    comments.Count);

                return Ok(comments);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while getting comments.");

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        success = false,
                        message =
                            "خطایی هنگام دریافت نظرات رخ داد."
                    });
            }
        }


        // =========================================================
        // CREATE COMMENT
        // POST: api/Comments
        // =========================================================

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateComment(
            [FromBody] CreateCommentRequest? request,
            CancellationToken cancellationToken)
        {
            try
            {
                // =================================================
                // REQUEST
                // =================================================

                if (request == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message =
                            "اطلاعات نظر ارسال نشده است."
                    });
                }


                // =================================================
                // GET USER ID FROM JWT
                // =================================================

                int userId = GetUserIdFromClaims();

                if (userId <= 0)
                {
                    _logger.LogWarning(
                        "Create comment failed because JWT user ID is invalid.");

                    return Unauthorized(new
                    {
                        success = false,
                        message =
                            "احراز هویت کاربر معتبر نیست."
                    });
                }


                // =================================================
                // RATING
                // =================================================

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


                // =================================================
                // TEXT
                // =================================================

                if (string.IsNullOrWhiteSpace(
                    request.Text))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message =
                            "متن نظر را وارد کنید."
                    });
                }


                string text =
                    request.Text.Trim();


                // =================================================
                // NORMALIZE SPACES
                // =================================================

                text = Regex.Replace(
                    text,
                    @"[ \t]+",
                    " ");


                // =================================================
                // MAX LENGTH
                // =================================================

                if (text.Length > MaxCommentLength)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message =
                            "متن نظر نمی‌تواند بیشتر از ۵۰۰ کاراکتر باشد."
                    });
                }


                // =================================================
                // DANGEROUS CONTENT
                // =================================================

                if (ContainsDangerousContent(text))
                {
                    _logger.LogWarning(
                        "Dangerous comment content detected. UserId={UserId}",
                        userId);

                    return BadRequest(new
                    {
                        success = false,
                        message =
                            "متن نظر شامل محتوای غیرمجاز است."
                    });
                }


                // =================================================
                // FIND USER
                // =================================================

                User? user =
                    await _context.Users
                        .FirstOrDefaultAsync(
                            x => x.Id == userId,
                            cancellationToken);

                if (user == null)
                {
                    _logger.LogWarning(
                        "Comment user not found. UserId={UserId}",
                        userId);

                    return NotFound(new
                    {
                        success = false,
                        message =
                            "کاربر پیدا نشد."
                    });
                }


                // =================================================
                // PHONE VERIFICATION
                // =================================================

                if (!user.IsPhoneVerified)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message =
                            "شماره موبایل کاربر تأیید نشده است."
                    });
                }


                // =================================================
                // CREATE COMMENT
                // =================================================

                Comment comment =
                    new Comment
                    {
                        UserId = user.Id,

                        Text = text,

                        Rating = request.Rating,

                        CreatedAt =
                            DateTime.UtcNow,

                        // مستقیماً تأیید می‌شود
                        IsApproved = true
                    };


                // =================================================
                // SAVE
                // =================================================

                _context.Comments.Add(comment);

                await _context.SaveChangesAsync(
                    cancellationToken);


                // =================================================
                // LOG
                // =================================================

                _logger.LogInformation(
                    "Comment created successfully. " +
                    "CommentId={CommentId}, UserId={UserId}, Rating={Rating}",
                    comment.Id,
                    user.Id,
                    comment.Rating);


                // =================================================
                // RESPONSE
                // =================================================

                return Ok(new
                {
                    success = true,

                    message =
                        "نظر شما با موفقیت ثبت شد.",

                    commentId =
                        comment.Id
                });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Database error while creating comment.");

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        success = false,
                        message =
                            "نظر در پایگاه داده ذخیره نشد."
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while creating comment.");

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        success = false,
                        message =
                            "خطایی هنگام ثبت نظر رخ داد."
                    });
            }
        }


        // =========================================================
        // GET USER ID FROM JWT
        // =========================================================

        private int GetUserIdFromClaims()
        {
            try
            {
                string? userIdValue =
                    User.FindFirstValue(
                        ClaimTypes.NameIdentifier);

                if (string.IsNullOrWhiteSpace(
                    userIdValue))
                {
                    userIdValue =
                        User.FindFirstValue("sub");
                }

                if (string.IsNullOrWhiteSpace(
                    userIdValue))
                {
                    userIdValue =
                        User.FindFirstValue("userId");
                }

                if (int.TryParse(
                    userIdValue,
                    out int userId))
                {
                    return userId;
                }

                return 0;
            }
            catch
            {
                return 0;
            }
        }


        // =========================================================
        // DANGEROUS CONTENT DETECTION
        // =========================================================

        private static bool ContainsDangerousContent(
            string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }


            // -----------------------------------------------------
            // HTML TAGS
            // -----------------------------------------------------

            if (Regex.IsMatch(
                text,
                @"<\s*/?\s*[a-zA-Z][^>]*>",
                RegexOptions.IgnoreCase |
                RegexOptions.CultureInvariant))
            {
                return true;
            }


            // -----------------------------------------------------
            // JAVASCRIPT
            // -----------------------------------------------------

            if (Regex.IsMatch(
                text,
                @"javascript\s*:",
                RegexOptions.IgnoreCase |
                RegexOptions.CultureInvariant))
            {
                return true;
            }


            // -----------------------------------------------------
            // DATA
            // -----------------------------------------------------

            if (Regex.IsMatch(
                text,
                @"data\s*:",
                RegexOptions.IgnoreCase |
                RegexOptions.CultureInvariant))
            {
                return true;
            }


            // -----------------------------------------------------
            // EVENT HANDLERS
            // onclick=
            // onerror=
            // onload=
            // onmouseover=
            // -----------------------------------------------------

            if (Regex.IsMatch(
                text,
                @"\bon[a-zA-Z]+\s*=",
                RegexOptions.IgnoreCase |
                RegexOptions.CultureInvariant))
            {
                return true;
            }


            return false;
        }
    }
}