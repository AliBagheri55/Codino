using System.Net.Http.Json;
using Codino.Models.Api;

namespace Codino.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;

        public ApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // =========================================================
        // LOGIN
        // =========================================================
public async Task<CourseResponse?> GetCourseBySlugAsync(
    string slug)
        {
            try
            {
                return await _httpClient
                    .GetFromJsonAsync<CourseResponse>(
                        $"api/courses/{slug}");
            }
            catch
            {
                return null;
            }
        }

        public async Task<LoginResponse?> LoginAsync(
            LoginRequest request)
        {
            try
            {
                HttpResponseMessage response =
                    await _httpClient.PostAsJsonAsync(
                        "api/Auth/login",
                        request);

                LoginResponse? result =
                    await response.Content
                        .ReadFromJsonAsync<LoginResponse>();

                return result ?? new LoginResponse
                {
                    Success = false,
                    Message = "پاسخ نامعتبر از سرور دریافت شد."
                };
            }
            catch (Exception ex)
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = $"خطای ارتباط با سرور: {ex.Message}"
                };
            }
        }

        // =========================================================
        // REGISTER
        // =========================================================

        public async Task<ApiResponse?> RegisterAsync(
            RegisterRequest request)
        {
            try
            {
                HttpResponseMessage response =
                    await _httpClient.PostAsJsonAsync(
                        "api/Auth/register",
                        request);

                ApiResponse? result =
                    await response.Content
                        .ReadFromJsonAsync<ApiResponse>();

                return result ?? new ApiResponse
                {
                    Success = false,
                    Message = "پاسخ نامعتبر از سرور دریافت شد."
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Success = false,
                    Message = $"خطای ارتباط با سرور: {ex.Message}"
                };
            }
        }

        // =========================================================
        // VERIFY REGISTER
        // =========================================================

        public async Task<ApiResponse?> VerifyRegisterAsync(
            string phoneNumber,
            string otp)
        {
            try
            {
                var request = new VerifyRegisterRequest
                {
                    PhoneNumber = phoneNumber,
                    Code = otp
                };

                HttpResponseMessage response =
                    await _httpClient.PostAsJsonAsync(
                        "api/Auth/verify-register",
                        request);

                ApiResponse? result =
                    await response.Content
                        .ReadFromJsonAsync<ApiResponse>();

                return result ?? new ApiResponse
                {
                    Success = false,
                    Message = "پاسخ نامعتبر از سرور دریافت شد."
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Success = false,
                    Message = $"خطای ارتباط با سرور: {ex.Message}"
                };
            }
        }

        // =========================================================
        // GET CAPTCHA
        // =========================================================

        public async Task<CaptchaResponse?> GetCaptchaAsync()
        {
            try
            {
                HttpResponseMessage response =
                    await _httpClient.GetAsync(
                        "api/Auth/captcha");

                CaptchaResponse? result =
                    await response.Content
                        .ReadFromJsonAsync<CaptchaResponse>();

                return result ?? new CaptchaResponse
                {
                    Success = false,
                    Message = "پاسخ کپچا از سرور نامعتبر است."
                };
            }
            catch (Exception ex)
            {
                return new CaptchaResponse
                {
                    Success = false,
                    Message = $"خطای ارتباط با سرور: {ex.Message}"
                };
            }
        }

        // =========================================================
        // FORGOT PASSWORD
        // =========================================================

        public async Task<ForgotPasswordResponse?>
            ForgotPasswordAsync(
                ForgotPasswordRequest request)
        {
            try
            {
                HttpResponseMessage response =
                    await _httpClient.PostAsJsonAsync(
                        "api/Auth/forgot-password",
                        request);

                ForgotPasswordResponse? result =
                    await response.Content
                        .ReadFromJsonAsync<ForgotPasswordResponse>();

                return result ?? new ForgotPasswordResponse
                {
                    Success = false,
                    Message = "پاسخ بازیابی رمز عبور نامعتبر است."
                };
            }
            catch (Exception ex)
            {
                return new ForgotPasswordResponse
                {
                    Success = false,
                    Message = $"خطای ارتباط با سرور: {ex.Message}"
                };
            }
        }

        // =========================================================
        // RESET PASSWORD
        // =========================================================

        public async Task<ApiResponse?>
            ResetPasswordAsync(
                ResetPasswordRequest request)
        {
            try
            {
                HttpResponseMessage response =
                    await _httpClient.PostAsJsonAsync(
                        "api/Auth/reset-password",
                        request);

                ApiResponse? result =
                    await response.Content
                        .ReadFromJsonAsync<ApiResponse>();

                return result ?? new ApiResponse
                {
                    Success = false,
                    Message = "پاسخ تغییر رمز عبور نامعتبر است."
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Success = false,
                    Message = $"خطای ارتباط با سرور: {ex.Message}"
                };
            }
        }

        // =========================================================
        // GET COMMENTS
        // =========================================================

        public async Task<List<CommentResponse>>
            GetCommentsAsync()
        {
            try
            {
                List<CommentResponse>? comments =
                    await _httpClient.GetFromJsonAsync<
                        List<CommentResponse>>(
                            "api/Comments");

                return comments ??
                       new List<CommentResponse>();
            }
            catch
            {
                return new List<CommentResponse>();
            }
        }

        // =========================================================
        // CREATE COMMENT
        // =========================================================

        public async Task<ApiResponse?>
            CreateCommentAsync(
                CreateCommentRequest request)
        {
            try
            {
                HttpResponseMessage response =
                    await _httpClient.PostAsJsonAsync(
                        "api/Comments",
                        request);

                ApiResponse? result =
                    await response.Content
                        .ReadFromJsonAsync<ApiResponse>();

                return result ?? new ApiResponse
                {
                    Success = false,
                    Message = "پاسخ نامعتبر از سرور دریافت شد."
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Success = false,
                    Message = $"خطای ارتباط با سرور: {ex.Message}"
                };
            }
        }
public async Task<List<CourseResponse>> GetCoursesAsync()
        {
            try
            {
                List<CourseResponse>? courses =
                    await _httpClient.GetFromJsonAsync<List<CourseResponse>>(
                        "api/courses");

                return courses ?? new List<CourseResponse>();
            }
            catch
            {
                return new List<CourseResponse>();
            }
        }

    }
}