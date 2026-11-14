namespace Codino.Models.Api
{
    /// <summary>
    /// درخواست تمدید Access Token.
    /// </summary>
    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}