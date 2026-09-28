using Codino.Models.Api;
using Codino.Services;
using Microsoft.AspNetCore.Mvc;

namespace Codino.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApiService _apiService;

        public AccountController(
            ApiService apiService)
        {
            _apiService = apiService;
        }

        // =========================================================
        // REGISTER - GET
        // =========================================================

        [HttpGet]
        public IActionResult Register()
        {
            GenerateCaptcha("Register");

            return View("Register");
        }

        // =========================================================
        // REGISTER - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            string fullName,
            string phoneNumber,
            string password,
            string repeatPassword,
            string captcha)
        {
            string? sessionCaptcha =
                HttpContext.Session.GetString(
                    "RegisterCaptcha");

            // -----------------------------------------------------
            // CHECK CAPTCHA
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                    sessionCaptcha) ||
                string.IsNullOrWhiteSpace(captcha) ||
                !string.Equals(
                    sessionCaptcha,
                    captcha.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.Error =
                    "کد کپچا صحیح نیست.";

                GenerateCaptcha("Register");

                return View("Register");
            }

            // -----------------------------------------------------
            // CHECK NAME
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(fullName))
            {
                ViewBag.Error =
                    "لطفاً نام و نام خانوادگی را وارد کنید.";

                GenerateCaptcha("Register");

                return View("Register");
            }

            // -----------------------------------------------------
            // CHECK PHONE
            // -----------------------------------------------------

            phoneNumber =
                phoneNumber?.Trim() ?? "";

            if (!IsValidPhoneNumber(phoneNumber))
            {
                ViewBag.Error =
                    "شماره موبایل معتبر نیست.";

                GenerateCaptcha("Register");

                return View("Register");
            }

            // -----------------------------------------------------
            // CHECK PASSWORD
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error =
                    "لطفاً رمز عبور را وارد کنید.";

                GenerateCaptcha("Register");

                return View("Register");
            }

            if (password.Length < 8)
            {
                ViewBag.Error =
                    "رمز عبور باید حداقل ۸ کاراکتر باشد.";

                GenerateCaptcha("Register");

                return View("Register");
            }

            // -----------------------------------------------------
            // CHECK PASSWORD MATCH
            // -----------------------------------------------------

            if (password != repeatPassword)
            {
                ViewBag.Error =
                    "رمز عبور و تکرار رمز عبور یکسان نیستند.";

                GenerateCaptcha("Register");

                return View("Register");
            }

            // -----------------------------------------------------
            // CREATE REQUEST
            // -----------------------------------------------------

            var request =
                new RegisterRequest
                {
                    FullName =
                        fullName.Trim(),

                    PhoneNumber =
                        phoneNumber,

                    Password =
                        password,

                    RepeatPassword =
                        repeatPassword
                };

            // -----------------------------------------------------
            // SEND TO API
            // -----------------------------------------------------

            ApiResponse? response =
                await _apiService.RegisterAsync(
                    request);

            if (response == null)
            {
                ViewBag.Error =
                    "ارتباط با سرور برقرار نشد.";

                GenerateCaptcha("Register");

                return View("Register");
            }

            if (!response.Success)
            {
                ViewBag.Error =
                    response.Message;

                GenerateCaptcha("Register");

                return View("Register");
            }

            // -----------------------------------------------------
            // REMOVE CAPTCHA
            // -----------------------------------------------------

            HttpContext.Session.Remove(
                "RegisterCaptcha");

            // -----------------------------------------------------
            // SAVE REGISTER DATA
            // -----------------------------------------------------

            HttpContext.Session.SetString(
                "RegisterPhone",
                phoneNumber);

            HttpContext.Session.SetString(
                "RegisterFullName",
                fullName.Trim());

            if (!string.IsNullOrWhiteSpace(
                    response.Otp))
            {
                HttpContext.Session.SetString(
                    "RegisterOtp",
                    response.Otp);
            }

            // -----------------------------------------------------
            // GO VERIFY PAGE
            // -----------------------------------------------------

            return RedirectToAction(
                nameof(VerifyRegisterPage));
        }

        // =========================================================
        // VERIFY REGISTER - GET
        // =========================================================

        [HttpGet]
        public IActionResult VerifyRegisterPage()
        {
            string? phoneNumber =
                HttpContext.Session.GetString(
                    "RegisterPhone");

            if (string.IsNullOrWhiteSpace(
                    phoneNumber))
            {
                return RedirectToAction(
                    nameof(Register));
            }

            ViewBag.PhoneNumber =
                phoneNumber;

            ViewBag.Otp =
                HttpContext.Session.GetString(
                    "RegisterOtp");

            return View("VerifyRegister");
        }

        // =========================================================
        // VERIFY REGISTER - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            VerifyRegister()
        {
            string? phoneNumber =
                HttpContext.Session.GetString(
                    "RegisterPhone");

            if (string.IsNullOrWhiteSpace(
                    phoneNumber))
            {
                return RedirectToAction(
                    nameof(Register));
            }

            if (!Request.HasFormContentType)
            {
                return ShowVerifyRegisterError(
                    phoneNumber,
                    "اطلاعات فرم دریافت نشد.");
            }

            var form =
                await Request.ReadFormAsync();

            string code =
                form["code"]
                    .ToString()
                    .Trim();

            if (string.IsNullOrWhiteSpace(code))
            {
                return ShowVerifyRegisterError(
                    phoneNumber,
                    "کد تأیید را وارد کنید.");
            }

            if (code.Length != 6)
            {
                return ShowVerifyRegisterError(
                    phoneNumber,
                    "کد تأیید باید ۶ رقم باشد.");
            }

            if (!code.All(char.IsDigit))
            {
                return ShowVerifyRegisterError(
                    phoneNumber,
                    "کد تأیید باید فقط شامل اعداد باشد.");
            }

            ApiResponse? response =
                await _apiService.VerifyRegisterAsync(
                    phoneNumber,
                    code);

            if (response == null)
            {
                return ShowVerifyRegisterError(
                    phoneNumber,
                    "ارتباط با سرور برقرار نشد.");
            }

            if (!response.Success)
            {
                return ShowVerifyRegisterError(
                    phoneNumber,
                    response.Message);
            }

            // -----------------------------------------------------
            // CLEAR REGISTER SESSION
            // -----------------------------------------------------

            HttpContext.Session.Remove(
                "RegisterPhone");

            HttpContext.Session.Remove(
                "RegisterFullName");

            HttpContext.Session.Remove(
                "RegisterOtp");

            // -----------------------------------------------------
            // SUCCESS
            // -----------------------------------------------------

            TempData["AccountSuccess"] =
                "ثبت‌نام با موفقیت انجام شد. اکنون وارد حساب خود شوید.";

            return RedirectToAction(
                nameof(Login));
        }

        // =========================================================
        // VERIFY REGISTER ERROR
        // =========================================================

        private IActionResult ShowVerifyRegisterError(
            string phoneNumber,
            string error)
        {
            ViewBag.Error =
                error;

            ViewBag.PhoneNumber =
                phoneNumber;

            ViewBag.Otp =
                HttpContext.Session.GetString(
                    "RegisterOtp");

            return View("VerifyRegister");
        }

        // =========================================================
        // LOGIN - GET
        // =========================================================

        [HttpGet]
        public IActionResult Login()
        {
            GenerateCaptcha("Login");

            return View("Login");
        }

        // =========================================================
        // LOGIN - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            string phoneNumber,
            string password,
            string captcha)
        {
            string? sessionCaptcha =
                HttpContext.Session.GetString(
                    "LoginCaptcha");

            // -----------------------------------------------------
            // CHECK CAPTCHA
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                    sessionCaptcha) ||
                string.IsNullOrWhiteSpace(captcha) ||
                !string.Equals(
                    sessionCaptcha,
                    captcha.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.Error =
                    "کد کپچا صحیح نیست.";

                GenerateCaptcha("Login");

                return View("Login");
            }

            phoneNumber =
                phoneNumber?.Trim() ?? "";

            // -----------------------------------------------------
            // CHECK PHONE
            // -----------------------------------------------------

            if (!IsValidPhoneNumber(phoneNumber))
            {
                ViewBag.Error =
                    "شماره موبایل معتبر نیست.";

                GenerateCaptcha("Login");

                return View("Login");
            }

            // -----------------------------------------------------
            // CHECK PASSWORD
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error =
                    "لطفاً رمز عبور را وارد کنید.";

                GenerateCaptcha("Login");

                return View("Login");
            }

            // -----------------------------------------------------
            // LOGIN REQUEST
            // -----------------------------------------------------

            var request =
                new LoginRequest
                {
                    PhoneNumber =
                        phoneNumber,

                    Password =
                        password
                };

            LoginResponse? response =
                await _apiService.LoginAsync(
                    request);

            if (response == null)
            {
                ViewBag.Error =
                    "ارتباط با سرور برقرار نشد.";

                GenerateCaptcha("Login");

                return View("Login");
            }

            if (!response.Success)
            {
                ViewBag.Error =
                    response.Message;

                GenerateCaptcha("Login");

                return View("Login");
            }

            // -----------------------------------------------------
            // REMOVE CAPTCHA
            // -----------------------------------------------------

            HttpContext.Session.Remove(
                "LoginCaptcha");

            // -----------------------------------------------------
            // SAVE USER SESSION
            // -----------------------------------------------------

            HttpContext.Session.SetString(
                "UserId",
                response.UserId?.ToString() ?? "");

            HttpContext.Session.SetString(
                "UserFullName",
                response.FullName ?? "");

            HttpContext.Session.SetString(
                "UserPhoneNumber",
                phoneNumber);

            return RedirectToAction(
                "Index",
                "Home");
        }

        // =========================================================
        // LOGOUT
        // =========================================================

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction(
                "Index",
                "Home");
        }

        // =========================================================
        // FORGOT PASSWORD - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult>
            ForgotPassword()
        {
            CaptchaResponse? captcha =
                await _apiService.GetCaptchaAsync();

            if (captcha == null ||
                !captcha.Success)
            {
                ViewBag.Error =
                    captcha?.Message ??
                    "دریافت کپچا امکان‌پذیر نیست.";

                return View("ForgotPassword");
            }

            ViewBag.CaptchaId =
                captcha.CaptchaId;

            ViewBag.CaptchaImage =
                captcha.Image;

            return View("ForgotPassword");
        }

        // =========================================================
        // FORGOT PASSWORD - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            ForgotPassword(
                string phoneNumber,
                string captchaId,
                string captcha)
        {
            phoneNumber =
                phoneNumber?.Trim() ?? "";

            captchaId =
                captchaId?.Trim() ?? "";

            captcha =
                captcha?.Trim() ?? "";

            // -----------------------------------------------------
            // CHECK PHONE
            // -----------------------------------------------------

            if (!IsValidPhoneNumber(phoneNumber))
            {
                return await ShowForgotPasswordError(
                    "شماره موبایل معتبر نیست.");
            }

            // -----------------------------------------------------
            // CHECK CAPTCHA
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                    captchaId))
            {
                return await ShowForgotPasswordError(
                    "شناسه کپچا معتبر نیست.");
            }

            if (string.IsNullOrWhiteSpace(
                    captcha))
            {
                return await ShowForgotPasswordError(
                    "کپچا را وارد کنید.");
            }

            // -----------------------------------------------------
            // SEND REQUEST TO API
            // -----------------------------------------------------

            var request =
                new ForgotPasswordRequest
                {
                    PhoneNumber =
                        phoneNumber,

                    CaptchaId =
                        captchaId,

                    Captcha =
                        captcha
                };

            ForgotPasswordResponse? response =
                await _apiService.ForgotPasswordAsync(
                    request);

            if (response == null)
            {
                return await ShowForgotPasswordError(
                    "ارتباط با سرور برقرار نشد.");
            }

            if (!response.Success)
            {
                return await ShowForgotPasswordError(
                    response.Message);
            }

            // -----------------------------------------------------
            // CHECK RESET TOKEN
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                    response.ResetToken))
            {
                return await ShowForgotPasswordError(
                    "توکن بازیابی دریافت نشد.");
            }

            // -----------------------------------------------------
            // GO RESET PASSWORD
            // -----------------------------------------------------

            return RedirectToAction(
                nameof(ResetPassword),
                new
                {
                    phoneNumber =
                        phoneNumber,

                    resetToken =
                        response.ResetToken
                });
        }

        // =========================================================
        // FORGOT PASSWORD ERROR
        // =========================================================

        private async Task<IActionResult>
            ShowForgotPasswordError(
                string error)
        {
            CaptchaResponse? captcha =
                await _apiService.GetCaptchaAsync();

            ViewBag.Error =
                error;

            if (captcha != null &&
                captcha.Success)
            {
                ViewBag.CaptchaId =
                    captcha.CaptchaId;

                ViewBag.CaptchaImage =
                    captcha.Image;
            }

            return View("ForgotPassword");
        }

        // =========================================================
        // RESET PASSWORD - GET
        // =========================================================

        [HttpGet]
        public IActionResult ResetPassword(
            string phoneNumber,
            string resetToken)
        {
            if (string.IsNullOrWhiteSpace(
                    phoneNumber) ||
                string.IsNullOrWhiteSpace(
                    resetToken))
            {
                return RedirectToAction(
                    nameof(ForgotPassword));
            }

            ViewBag.PhoneNumber =
                phoneNumber;

            ViewBag.ResetToken =
                resetToken;

            return View("ResetPassword");
        }

        // =========================================================
        // RESET PASSWORD - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            ResetPassword(
                string phoneNumber,
                string resetToken,
                string newPassword,
                string repeatPassword)
        {
            phoneNumber =
                phoneNumber?.Trim() ?? "";

            resetToken =
                resetToken?.Trim() ?? "";

            newPassword =
                newPassword ?? "";

            repeatPassword =
                repeatPassword ?? "";

            // -----------------------------------------------------
            // CHECK PHONE
            // -----------------------------------------------------

            if (!IsValidPhoneNumber(phoneNumber))
            {
                return ShowResetPasswordError(
                    phoneNumber,
                    resetToken,
                    "شماره موبایل معتبر نیست.");
            }

            // -----------------------------------------------------
            // CHECK TOKEN
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                    resetToken))
            {
                return RedirectToAction(
                    nameof(ForgotPassword));
            }

            // -----------------------------------------------------
            // CHECK PASSWORD
            // -----------------------------------------------------

            if (newPassword.Length < 8)
            {
                return ShowResetPasswordError(
                    phoneNumber,
                    resetToken,
                    "رمز عبور باید حداقل ۸ کاراکتر باشد.");
            }

            // -----------------------------------------------------
            // CHECK PASSWORD MATCH
            // -----------------------------------------------------

            if (newPassword !=
                repeatPassword)
            {
                return ShowResetPasswordError(
                    phoneNumber,
                    resetToken,
                    "رمزهای عبور یکسان نیستند.");
            }

            // -----------------------------------------------------
            // SEND TO API
            // -----------------------------------------------------

            var request =
                new ResetPasswordRequest
                {
                    PhoneNumber =
                        phoneNumber,

                    ResetToken =
                        resetToken,

                    NewPassword =
                        newPassword,

                    RepeatPassword =
                        repeatPassword
                };

            ApiResponse? response =
                await _apiService.ResetPasswordAsync(
                    request);

            if (response == null)
            {
                return ShowResetPasswordError(
                    phoneNumber,
                    resetToken,
                    "ارتباط با سرور برقرار نشد.");
            }

            if (!response.Success)
            {
                return ShowResetPasswordError(
                    phoneNumber,
                    resetToken,
                    response.Message);
            }

            // -----------------------------------------------------
            // SUCCESS
            // -----------------------------------------------------

            TempData["AccountSuccess"] =
                "رمز عبور با موفقیت تغییر کرد. اکنون وارد حساب خود شوید.";

            return RedirectToAction(
                nameof(Login));
        }

        // =========================================================
        // RESET PASSWORD ERROR
        // =========================================================

        private IActionResult
            ShowResetPasswordError(
                string phoneNumber,
                string resetToken,
                string error)
        {
            ViewBag.PhoneNumber =
                phoneNumber;

            ViewBag.ResetToken =
                resetToken;

            ViewBag.Error =
                error;

            return View("ResetPassword");
        }

        // =========================================================
        // MVC CAPTCHA
        // =========================================================

        private void GenerateCaptcha(
            string type)
        {
            const string characters =
                "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

            Random random =
                new Random();

            string captcha =
                "";

            for (int i = 0; i < 5; i++)
            {
                captcha +=
                    characters[
                        random.Next(
                            characters.Length)];
            }

            if (type == "Login")
            {
                HttpContext.Session.SetString(
                    "LoginCaptcha",
                    captcha);
            }
            else
            {
                HttpContext.Session.SetString(
                    "RegisterCaptcha",
                    captcha);
            }

            ViewBag.Captcha =
                captcha;
        }

        // =========================================================
        // PHONE VALIDATION
        // =========================================================

        private bool IsValidPhoneNumber(
            string phoneNumber)
        {
            return
                phoneNumber.StartsWith("09") &&
                phoneNumber.Length == 11 &&
                phoneNumber.All(char.IsDigit);
        }
    }
}