namespace Codino.Models.Api
{
    public class LoginResponse
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public int? UserId { get; set; }

        public string? FullName { get; set; }

        public string? Token { get; set; }
    }
}