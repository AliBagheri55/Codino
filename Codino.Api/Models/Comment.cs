namespace Codino.Api.Models
{
    public class Comment
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string Text { get; set; } = string.Empty;

        public int Rating { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool IsApproved { get; set; }

        public User? User { get; set; }
    }
}