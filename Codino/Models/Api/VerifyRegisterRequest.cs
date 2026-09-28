namespace Codino.Models.Api
{
    public class VerifyRegisterRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;
    }
}