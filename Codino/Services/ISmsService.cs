namespace Codino.Services
{
    public interface ISmsService
    {
        Task<bool> SendOtpAsync(string phoneNumber, string code);
    }
}