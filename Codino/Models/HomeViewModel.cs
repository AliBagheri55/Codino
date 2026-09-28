using Codino.Models.Api;

namespace Codino.Models
{
    public class HomeViewModel
    {
        public List<CourseResponse> Courses { get; set; } = new();

        public List<CommentResponse> Comments { get; set; } = new();
    }
}