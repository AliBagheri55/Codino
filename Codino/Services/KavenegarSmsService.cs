using System.Text.Json;

namespace Codino.Services
{
    public class KavenegarSmsService : ISmsService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public KavenegarSmsService(
            IConfiguration configuration,
            HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public async Task<bool> SendOtpAsync(
            string phoneNumber,
            string code)
        {
            string? apiKey =
                _configuration["Kavenegar:ApiKey"];

            string? template =
                _configuration["Kavenegar:Template"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Console.WriteLine("KAVENEGAR ERROR: API KEY خالی است.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(template))
            {
                Console.WriteLine("KAVENEGAR ERROR: TEMPLATE خالی است.");
                return false;
            }

            string url =
                $"https://api.kavenegar.com/v1/{apiKey}/verify/lookup.json" +
                $"?receptor={Uri.EscapeDataString(phoneNumber)}" +
                $"&token={Uri.EscapeDataString(code)}" +
                $"&template={Uri.EscapeDataString(template)}" +
                $"&type=sms";

            Console.WriteLine("====================================");
            Console.WriteLine("KAVENEGAR OTP");
            Console.WriteLine($"Phone: {phoneNumber}");
            Console.WriteLine($"Template: {template}");
            Console.WriteLine($"API Key exists: {!string.IsNullOrWhiteSpace(apiKey)}");
            Console.WriteLine("====================================");

            try
            {
                HttpResponseMessage response =
                    await _httpClient.GetAsync(url);

                string result =
                    await response.Content.ReadAsStringAsync();

                Console.WriteLine("KAVENEGAR HTTP STATUS:");
                Console.WriteLine((int)response.StatusCode);

                Console.WriteLine("KAVENEGAR RESPONSE:");
                Console.WriteLine(result);

                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                using JsonDocument json =
                    JsonDocument.Parse(result);

                if (!json.RootElement.TryGetProperty(
                        "return",
                        out JsonElement returnElement))
                {
                    Console.WriteLine(
                        "KAVENEGAR ERROR: return در پاسخ وجود ندارد.");

                    return false;
                }

                int status =
                    returnElement
                        .GetProperty("status")
                        .GetInt32();

                string message =
                    returnElement
                        .GetProperty("message")
                        .GetString() ?? "";

                Console.WriteLine(
                    $"KAVENEGAR STATUS: {status}");

                Console.WriteLine(
                    $"KAVENEGAR MESSAGE: {message}");

                return status == 200;
            }
            catch (Exception ex)
            {
                Console.WriteLine("KAVENEGAR EXCEPTION:");
                Console.WriteLine(ex.Message);

                return false;
            }
        }
    }
}