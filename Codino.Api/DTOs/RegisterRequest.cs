namespace Codino.Api.DTOs
{
    public class RegisterRequest
    {
        public string FullName { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string RepeatPassword { get; set; } = string.Empty;
    }
}