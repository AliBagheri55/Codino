namespace Codino.Api.DTOs
{
    public class ApiResponse
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public string? Otp { get; set; }

        public int? UserId { get; set; }

        public string? FullName { get; set; }
    }
}