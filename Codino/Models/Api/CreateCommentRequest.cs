namespace Codino.Models.Api
{
    public class CreateCommentRequest
    {
        public int UserId { get; set; }

        public string Text { get; set; }
            = string.Empty;

        public int Rating { get; set; }
    }
}