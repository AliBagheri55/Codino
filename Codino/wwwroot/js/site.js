
document.addEventListener("DOMContentLoaded", function () {

    /* =====================================
       MOBILE MENU
    ===================================== */

    const menuButton = document.getElementById("mobileMenuButton");
    const navMenu = document.getElementById("navMenu");

    if (menuButton && navMenu) {

        menuButton.addEventListener("click", function () {

            const isOpen = navMenu.classList.toggle("active");

            menuButton.setAttribute("aria-expanded", String(isOpen));
            menuButton.setAttribute(
                "aria-label",
                isOpen ? "بستن منو" : "باز کردن منو"
            );   

            menuButton.textContent = isOpen ? "✕" : "☰";
        });

        // بستن منو بعد از انتخاب لینک
        navMenu.querySelectorAll("a").forEach(function (link) {

            link.addEventListener("click", function () {

                navMenu.classList.remove("active");

                menuButton.setAttribute("aria-expanded", "false");
                menuButton.setAttribute("aria-label", "باز کردن منو");
                menuButton.textContent = "☰";

            });

        });

        // بستن منو با کلیک خارج از آن
        document.addEventListener("click", function (event) {

            const clickedInsideMenu = navMenu.contains(event.target);
            const clickedButton = menuButton.contains(event.target);

            if (!clickedInsideMenu && !clickedButton) {

                navMenu.classList.remove("active");

                menuButton.setAttribute("aria-expanded", "false");
                menuButton.textContent = "☰";

            }

        });

    }


    /* =====================================
       SMOOTH SCROLL
    ===================================== */

    document.querySelectorAll('a[href^="#"]').forEach(function (link) {

        link.addEventListener("click", function (event) {

            const targetId = this.getAttribute("href");

            if (!targetId || targetId === "#") {
                return;
            }

            const target = document.querySelector(targetId);

            if (target) {

                event.preventDefault();

                target.scrollIntoView({
                    behavior: "smooth",
                    block: "start"
                });

            }

        });

    });


    /* =====================================
       COMMENT RATING
    ===================================== */

    const ratingButtons = document.querySelectorAll(
        "#ratingStars button"
    );

    const ratingInput = document.getElementById("rating");

    function updateRating(selectedRating) {

        ratingButtons.forEach(function (button) {

            const buttonRating = Number(button.dataset.rating);

            button.classList.toggle(
                "active",
                buttonRating <= selectedRating
            );

            button.setAttribute(
                "aria-pressed",
                String(buttonRating === selectedRating)
            );

        });

        if (ratingInput) {
            ratingInput.value = String(selectedRating);
        }

    }

    ratingButtons.forEach(function (button) {

        button.addEventListener("click", function () {

            const selectedRating = Number(this.dataset.rating);

            if (selectedRating >= 1 && selectedRating <= 5) {
                updateRating(selectedRating);
            }

        });

    });


    /* =====================================
       COMMENT CHARACTER COUNTER
    ===================================== */

    const commentText = document.getElementById("commentText");
    const commentCounter = document.getElementById("commentCounter");

    if (commentText && commentCounter) {

        function updateCommentCounter() {

            commentCounter.textContent = String(
                commentText.value.length
            );

        }

        commentText.addEventListener(
            "input",
            updateCommentCounter
        );

        updateCommentCounter();

    }


    /* =====================================
       COMMENT FORM VALIDATION
    ===================================== */

    const commentForm = document.getElementById("commentForm");

    if (commentForm) {

        commentForm.addEventListener("submit", function (event) {

            const textArea = document.getElementById("commentText");
            const rating = document.getElementById("rating");

            if (!textArea || !rating) {
                return;
            }

            if (!textArea.value.trim()) {

                event.preventDefault();

                alert("لطفاً متن نظر خود را وارد کنید.");

                textArea.focus();

                return;
            }

            if (Number(rating.value) < 1 || Number(rating.value) > 5) {

                event.preventDefault();

                alert("لطفاً امتیاز خود را از ۱ تا ۵ انتخاب کنید.");

                return;
            }

            if (textArea.value.length > 500) {

                event.preventDefault();

                alert("متن نظر نمی‌تواند بیشتر از ۵۰۰ کاراکتر باشد.");

                return;
            }

        });

    }


    /* =====================================
       TRACKING FORM VALIDATION
       در صورت وجود فرم پیگیری
    ===================================== */

    const trackingForm = document.querySelector(".tracking-form");

    if (trackingForm) {

        trackingForm.addEventListener("submit", function (event) {

            const trackingInput = trackingForm.querySelector(
                'input[name="trackingCode"]'
            );

            if (!trackingInput || !trackingInput.value.trim()) {

                event.preventDefault();

                alert("لطفاً کد پیگیری پروژه را وارد کنید.");

                if (trackingInput) {
                    trackingInput.focus();
                }

            }

        });

    }


    /* =====================================
       PREVENT HIDDEN SECTIONS
    ===================================== */

    document.querySelectorAll(
        "main section, .codino-announcements, .footer"
    ).forEach(function (section) {

        section.style.visibility = "visible";

    });

    document.querySelectorAll(
        ".reveal, .reveal-item, [data-reveal], .scroll-reveal"
    ).forEach(function (element) {

        element.style.opacity = "1";
        element.style.visibility = "visible";
        element.style.transform = "none";

    });


    /* =====================================
       COMMENT MESSAGE AUTO HIGHLIGHT
    ===================================== */

    document.querySelectorAll(".comment-message").forEach(function (message) {

        message.scrollIntoView({
            behavior: "smooth",
            block: "center"
        });

    });

});

document.addEventListener("DOMContentLoaded", function () {

    "use strict";

    // دریافت عناصر فرم
    const phoneInput = document.getElementById("phoneNumber");
    const securityCodeInput = document.getElementById("securityCode");
    const captchaInput = document.getElementById("captcha");
    const form = document.querySelector(
        'form[asp-action="ForgotPassword"]'
    );

    // فقط اعداد انگلیسی
    function onlyDigits(value) {
        return value
            .replace(/[۰-۹]/g, function (digit) {
                return String("۰۱۲۳۴۵۶۷۸۹".indexOf(digit));
            })
            .replace(/[٠-٩]/g, function (digit) {
                return String("٠١٢٣٤٥٦٧٨٩".indexOf(digit));
            })
            .replace(/\D/g, "");
    }

    // شماره موبایل
    if (phoneInput) {

        phoneInput.addEventListener("input", function () {

            this.value = onlyDigits(this.value).slice(0, 11);

        });

        phoneInput.addEventListener("blur", function () {

            if (this.value.length > 0 &&
                !/^09\d{9}$/.test(this.value)) {

                this.setCustomValidity(
                    "شماره موبایل باید با 09 شروع شود و 11 رقم باشد."
                );

            } else {

                this.setCustomValidity("");

            }

        });

        phoneInput.addEventListener("input", function () {
            this.setCustomValidity("");
        });
    }

    // کد امنیتی چهار رقمی
    if (securityCodeInput) {

        securityCodeInput.addEventListener("input", function () {

            this.value = onlyDigits(this.value).slice(0, 4);

            this.setCustomValidity("");

        });

        securityCodeInput.addEventListener("blur", function () {

            if (this.value.length > 0 &&
                !/^\d{4}$/.test(this.value)) {

                this.setCustomValidity(
                    "کد امنیتی باید دقیقاً چهار رقم باشد."
                );

            } else {

                this.setCustomValidity("");

            }

        });
    }

    // کپچا
    if (captchaInput) {

        captchaInput.addEventListener("input", function () {

            this.value = this.value
                .replace(/\s/g, "")
                .toUpperCase()
                .slice(0, 6);

        });

    }

    // اعتبارسنجی قبل از ارسال فرم
    if (form) {

        form.addEventListener("submit", function (event) {

            if (phoneInput &&
                !/^09\d{9}$/.test(phoneInput.value)) {

                event.preventDefault();

                phoneInput.setCustomValidity(
                    "شماره موبایل معتبر نیست."
                );

                phoneInput.reportValidity();
                phoneInput.focus();

                return;
            }

            if (securityCodeInput &&
                !/^\d{4}$/.test(securityCodeInput.value)) {

                event.preventDefault();

                securityCodeInput.setCustomValidity(
                    "کد امنیتی باید دقیقاً چهار رقم باشد."
                );

                securityCodeInput.reportValidity();
                securityCodeInput.focus();

                return;
            }

            if (captchaInput &&
                captchaInput.value.trim().length === 0) {

                event.preventDefault();

                captchaInput.setCustomValidity(
                    "وارد کردن کد تصویر الزامی است."
                );

                captchaInput.reportValidity();
                captchaInput.focus();

                return;
            }

        });

    }

});

document.addEventListener("DOMContentLoaded", function () {

    "use strict";

    const phoneInput = document.getElementById("phoneNumber");
    const captchaInput = document.getElementById("captcha");
    const refreshButton = document.querySelector(".captcha-refresh");
    const loginForm = document.querySelector(
        'form[method="post"]'
    );

    // تبدیل اعداد فارسی و عربی به انگلیسی
    function normalizeDigits(value) {

        return value
            .replace(/[۰-۹]/g, function (digit) {
                return String("۰۱۲۳۴۵۶۷۸۹".indexOf(digit));
            })
            .replace(/[٠-٩]/g, function (digit) {
                return String("٠١٢٣٤٥٦٧٨٩".indexOf(digit));
            })
            .replace(/\D/g, "");

    }

    // اعتبارسنجی شماره موبایل
    if (phoneInput) {

        phoneInput.addEventListener("input", function () {

            this.value = normalizeDigits(this.value).slice(0, 11);
            this.setCustomValidity("");

        });

        phoneInput.addEventListener("blur", function () {

            if (this.value && !/^09\d{9}$/.test(this.value)) {

                this.setCustomValidity(
                    "شماره موبایل باید با 09 شروع شود و 11 رقم باشد."
                );

            } else {

                this.setCustomValidity("");

            }

        });

    }

    // مدیریت کپچا
    if (captchaInput) {

        captchaInput.addEventListener("input", function () {

            this.value = this.value
                .replace(/\s/g, "")
                .toUpperCase()
                .slice(0, 5);

            this.setCustomValidity("");

        });

    }

    // جلوگیری از چندبار کلیک روی دکمه تازه‌سازی
    if (refreshButton) {

        refreshButton.addEventListener("click", function () {

            this.disabled = true;

        });

    }

    // اعتبارسنجی فرم ورود
    if (loginForm) {

        loginForm.addEventListener("submit", function (event) {

            if (phoneInput &&
                !/^09\d{9}$/.test(phoneInput.value)) {

                event.preventDefault();

                phoneInput.setCustomValidity(
                    "شماره موبایل معتبر نیست."
                );

                phoneInput.reportValidity();
                phoneInput.focus();

                return;

            }

            if (captchaInput &&
                captchaInput.value.trim().length !== 5) {

                event.preventDefault();

                captchaInput.setCustomValidity(
                    "کد امنیتی باید دقیقاً 5 کاراکتر باشد."
                );

                captchaInput.reportValidity();
                captchaInput.focus();

                return;

            }

            const passwordInput = document.getElementById("password");

            if (passwordInput && passwordInput.value.length < 8) {

                event.preventDefault();

                passwordInput.setCustomValidity(
                    "رمز عبور باید حداقل 8 کاراکتر باشد."
                );

                passwordInput.reportValidity();
                passwordInput.focus();

                return;

            }

        });

    }

});

document.addEventListener("DOMContentLoaded", function () {

    "use strict";

    const logoutForm = document.querySelector(
        '.profile-card form[asp-action="Logout"]'
    );

    const logoutButton = document.querySelector(
        ".profile-logout-button"
    );

    if (logoutForm && logoutButton) {

        logoutForm.addEventListener("submit", function () {

            logoutButton.disabled = true;
            logoutButton.textContent = "در حال خروج...";

        });

    }

});

document.addEventListener("DOMContentLoaded", function () {

    "use strict";

    // دریافت عناصر فرم
    const form = document.querySelector(
        '.account-card form[method="post"]'
    );

    const fullNameInput = document.getElementById("fullName");
    const phoneInput = document.getElementById("phoneNumber");
    const passwordInput = document.getElementById("password");
    const repeatPasswordInput = document.getElementById("repeatPassword");
    const captchaInput = document.getElementById("captcha");

    // تبدیل اعداد فارسی و عربی به انگلیسی
    function normalizeDigits(value) {

        return value
            .replace(/[۰-۹]/g, function (digit) {
                return String("۰۱۲۳۴۵۶۷۸۹".indexOf(digit));
            })
            .replace(/[٠-٩]/g, function (digit) {
                return String("٠١٢٣٤٥٦٧٨٩".indexOf(digit));
            })
            .replace(/\D/g, "");

    }

    // شماره موبایل
    if (phoneInput) {

        phoneInput.addEventListener("input", function () {

            this.value = normalizeDigits(this.value).slice(0, 11);
            this.setCustomValidity("");

        });

        phoneInput.addEventListener("blur", function () {

            if (this.value && !/^09\d{9}$/.test(this.value)) {

                this.setCustomValidity(
                    "شماره موبایل باید با 09 شروع شود و 11 رقم باشد."
                );

            } else {

                this.setCustomValidity("");

            }

        });

    }

    // نام و نام خانوادگی
    if (fullNameInput) {

        fullNameInput.addEventListener("input", function () {

            this.value = this.value
                .replace(/\s{2,}/g, " ")
                .slice(0, 100);

            this.setCustomValidity("");

        });

        fullNameInput.addEventListener("blur", function () {

            if (this.value.trim().length < 3) {

                this.setCustomValidity(
                    "نام و نام خانوادگی باید حداقل 3 کاراکتر باشد."
                );

            } else {

                this.setCustomValidity("");

            }

        });

    }

    // کپچا
    if (captchaInput) {

        captchaInput.addEventListener("input", function () {

            this.value = this.value
                .replace(/\s/g, "")
                .toUpperCase()
                .slice(0, 5);

            this.setCustomValidity("");

        });

    }

    // پیام تطابق رمز عبور
    let passwordMessage = null;

    if (repeatPasswordInput) {

        passwordMessage = document.createElement("small");

        passwordMessage.className = "password-match-message";

        passwordMessage.setAttribute("aria-live", "polite");

        repeatPasswordInput.parentElement.appendChild(passwordMessage);

    }

    // بررسی یکسان بودن رمزها
    function checkPasswords() {

        if (!passwordInput || !repeatPasswordInput) {
            return true;
        }

        const password = passwordInput.value;
        const repeatedPassword = repeatPasswordInput.value;

        repeatPasswordInput.setCustomValidity("");

        if (!repeatedPassword) {

            passwordMessage.textContent = "";
            passwordMessage.className = "password-match-message";

            return true;

        }

        if (password !== repeatedPassword) {

            passwordMessage.textContent =
                "رمز عبور و تکرار آن یکسان نیستند.";

            passwordMessage.className =
                "password-match-message error";

            repeatPasswordInput.setCustomValidity(
                "رمز عبور و تکرار آن یکسان نیستند."
            );

            return false;

        }

        passwordMessage.textContent =
            "رمز عبور و تکرار آن یکسان هستند.";

        passwordMessage.className =
            "password-match-message success";

        return true;

    }

    if (passwordInput && repeatPasswordInput) {

        passwordInput.addEventListener("input", checkPasswords);

        repeatPasswordInput.addEventListener("input", checkPasswords);

    }

    // تازه‌سازی کپچا
    window.refreshRegisterCaptcha = function () {

        window.location.href = window.location.pathname;

    };

    // اعتبارسنجی نهایی فرم
    if (form) {

        form.addEventListener("submit", function (event) {

            // بررسی نام
            if (fullNameInput &&
                fullNameInput.value.trim().length < 3) {

                event.preventDefault();

                fullNameInput.setCustomValidity(
                    "نام و نام خانوادگی معتبر نیست."
                );

                fullNameInput.reportValidity();
                fullNameInput.focus();

                return;

            }

            // بررسی شماره موبایل
            if (phoneInput &&
                !/^09\d{9}$/.test(phoneInput.value)) {

                event.preventDefault();

                phoneInput.setCustomValidity(
                    "شماره موبایل معتبر نیست."
                );

                phoneInput.reportValidity();
                phoneInput.focus();

                return;

            }

            // بررسی رمز عبور
            if (passwordInput &&
                passwordInput.value.length < 8) {

                event.preventDefault();

                passwordInput.setCustomValidity(
                    "رمز عبور باید حداقل 8 کاراکتر باشد."
                );

                passwordInput.reportValidity();
                passwordInput.focus();

                return;

            }

            // بررسی تطابق رمزها
            if (!checkPasswords()) {

                event.preventDefault();

                repeatPasswordInput.reportValidity();
                repeatPasswordInput.focus();

                return;

            }

            // بررسی کپچا
            if (captchaInput &&
                captchaInput.value.length !== 5) {

                event.preventDefault();

                captchaInput.setCustomValidity(
                    "کد امنیتی باید دقیقاً 5 کاراکتر باشد."
                );

                captchaInput.reportValidity();
                captchaInput.focus();

                return;

            }

        });

    }

});

document.addEventListener("DOMContentLoaded", function () {

    const codeInput = document.getElementById("code");
    const form = codeInput?.closest("form");
    const submitButton = form?.querySelector(
        'button[type="submit"]'
    );

    if (!codeInput || !form) {
        return;
    }

    // تبدیل اعداد فارسی و عربی به انگلیسی
    function normalizeDigits(value) {
        return value
            .replace(/[۰-۹]/g, digit =>
                String(digit.charCodeAt(0) - 1776)
            )
            .replace(/[٠-٩]/g, digit =>
                String(digit.charCodeAt(0) - 1632)
            );
    }

    // فقط شش رقم مجاز است
    function cleanCode(value) {
        return normalizeDigits(value)
            .replace(/\D/g, "")
            .slice(0, 6);
    }

    // پاک‌سازی ورودی
    codeInput.addEventListener("input", function () {

        const cleaned = cleanCode(this.value);

        this.value = cleaned;

        this.classList.toggle(
            "is-invalid",
            cleaned.length > 0 && cleaned.length !== 6
        );
    });

    // مدیریت Paste
    codeInput.addEventListener("paste", function (event) {

        event.preventDefault();

        const clipboard =
            event.clipboardData || window.clipboardData;

        if (!clipboard) {
            return;
        }

        this.value = cleanCode(
            clipboard.getData("text")
        );

        this.dispatchEvent(
            new Event("input", { bubbles: true })
        );
    });

    // جلوگیری از Drag & Drop
    codeInput.addEventListener("drop", function (event) {
        event.preventDefault();
    });

    codeInput.addEventListener("dragover", function (event) {
        event.preventDefault();
    });

    // اعتبارسنجی هنگام ارسال فرم
    form.addEventListener("submit", function (event) {

        const code = cleanCode(codeInput.value);

        codeInput.value = code;

        if (code.length !== 6) {

            event.preventDefault();

            codeInput.classList.add("is-invalid");
            codeInput.focus();

            return;
        }

        if (submitButton) {
            submitButton.disabled = true;
            submitButton.textContent = "در حال بررسی...";
        }
    });

    // تمرکز خودکار روی فیلد کد
    codeInput.focus();

});

document.addEventListener("DOMContentLoaded", function () {

    const codeInput = document.getElementById("code");
    const form = codeInput?.closest("form");
    const submitButton = form?.querySelector(
        'button[type="submit"]'
    );

    if (!codeInput || !form) {
        return;
    }

    // تبدیل اعداد فارسی و عربی به انگلیسی
    function normalizeDigits(value) {
        return value
            .replace(/[۰-۹]/g, digit =>
                String(digit.charCodeAt(0) - 1776)
            )
            .replace(/[٠-٩]/g, digit =>
                String(digit.charCodeAt(0) - 1632)
            );
    }

    // فقط چهار رقم مجاز است
    function cleanCode(value) {
        return normalizeDigits(value)
            .replace(/\D/g, "")
            .slice(0, 4);
    }

    // پاک‌سازی ورودی
    codeInput.addEventListener("input", function () {

        this.value = cleanCode(this.value);

        this.classList.toggle(
            "is-invalid",
            this.value.length > 0 && this.value.length !== 4
        );
    });

    // مدیریت Paste
    codeInput.addEventListener("paste", function (event) {

        event.preventDefault();

        const clipboard =
            event.clipboardData || window.clipboardData;

        if (!clipboard) {
            return;
        }

        this.value = cleanCode(
            clipboard.getData("text")
        );

        this.dispatchEvent(
            new Event("input", { bubbles: true })
        );
    });

    // جلوگیری از Drag & Drop
    codeInput.addEventListener("drop", function (event) {
        event.preventDefault();
    });

    codeInput.addEventListener("dragover", function (event) {
        event.preventDefault();
    });

    // بررسی کد قبل از ارسال
    form.addEventListener("submit", function (event) {

        const code = cleanCode(codeInput.value);

        codeInput.value = code;

        if (code.length !== 4) {

            event.preventDefault();

            codeInput.classList.add("is-invalid");
            codeInput.focus();

            return;
        }

        // جلوگیری از ارسال چندباره فرم
        if (submitButton) {
            submitButton.disabled = true;
            submitButton.textContent = "در حال تأیید...";
        }
    });

    // تمرکز خودکار روی کد
    codeInput.focus();

});
