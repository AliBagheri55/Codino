namespace Codino.Api.Models
{
    public class Course
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Image { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public int Chapters { get; set; }

        public int Sessions { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public string Slug { get; set; } = string.Empty;
    }
}