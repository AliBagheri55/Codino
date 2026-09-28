namespace Codino.Api.DTOs
{
    public class CommentResponse
    {
        public int Id { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;

        public int Rating { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}