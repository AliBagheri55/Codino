
document.addEventListener("DOMContentLoaded", () => {
    const form = document.getElementById("collaborationForm");
    const submitButton = document.getElementById("submitButton");
    const successMessage = document.getElementById("successMessage");
    const newRequestButton = document.getElementById("newRequestButton");

    const specialtyInput = document.getElementById("specialty");
    const experienceInput = document.getElementById("experience");
    const skillsInput = document.getElementById("skills");
    const portfolioInput = document.getElementById("portfolio");
    const descriptionInput = document.getElementById("description");

    if (!form || !submitButton || !successMessage) {
        console.error("عناصر فرم همکاری پیدا نشدند.");
        return;
    }

    // آدرس کنترلر MVC؛ توکن ورود در سمت سرور مدیریت می‌شود.
    const submitUrl = form.dataset.submitUrl;

    function showError(message) {
        let errorElement = document.getElementById("collaborationError");

        if (!errorElement) {
            errorElement = document.createElement("p");
            errorElement.id = "collaborationError";
            errorElement.className = "form-error";
            errorElement.setAttribute("role", "alert");
            submitButton.insertAdjacentElement("beforebegin", errorElement);
        }

        errorElement.textContent = message;
        errorElement.hidden = false;
    }

    function clearError() {
        const errorElement = document.getElementById("collaborationError");

        if (errorElement) {
            errorElement.textContent = "";
            errorElement.hidden = true;
        }
    }

    function validateForm() {
        if (!specialtyInput.value.trim()) {
            showError("لطفاً حوزه تخصصی خود را انتخاب کنید.");
            specialtyInput.focus();
            return false;
        }

        if (!experienceInput.value.trim()) {
            showError("لطفاً سابقه فعالیت خود را انتخاب کنید.");
            experienceInput.focus();
            return false;
        }

        if (!skillsInput.value.trim()) {
            showError("لطفاً مهارت‌های خود را وارد کنید.");
            skillsInput.focus();
            return false;
        }

        if (!descriptionInput.value.trim()) {
            showError("لطفاً توضیحاتی درباره خود وارد کنید.");
            descriptionInput.focus();
            return false;
        }

        if (portfolioInput.value.trim()) {
            try {
                const url = new URL(portfolioInput.value.trim());

                if (url.protocol !== "https:" && url.protocol !== "http:") {
                    throw new Error("Invalid URL");
                }
            } catch {
                showError("لینک نمونه‌کار معتبر نیست.");
                portfolioInput.focus();
                return false;
            }
        }

        return true;
    }

    function setLoading(isLoading) {
        submitButton.disabled = isLoading;
        submitButton.textContent = isLoading
            ? "در حال ثبت درخواست..."
            : "ثبت درخواست همکاری";
    }

    form.addEventListener("submit", async (event) => {
        event.preventDefault();
        clearError();

        if (!validateForm()) {
            return;
        }

        if (!submitUrl) {
            showError("آدرس ثبت درخواست همکاری تنظیم نشده است.");
            return;
        }

        const tokenInput = form.querySelector(
            'input[name="__RequestVerificationToken"]'
        );

        const collaborationData = {
            specialty: specialtyInput.value.trim(),
            experience: experienceInput.value.trim(),
            skills: skillsInput.value.trim(),
            portfolioUrl: portfolioInput.value.trim() || null,
            description: descriptionInput.value.trim()
        };

        try {
            setLoading(true);

            const headers = {
                "Content-Type": "application/json"
            };

            if (tokenInput) {
                headers["RequestVerificationToken"] = tokenInput.value;
            }

            const response = await fetch(submitUrl, {
                method: "POST",
                headers,
                credentials: "same-origin",
                body: JSON.stringify(collaborationData)
            });

            const result = await response.json();

            if (!response.ok || result.success !== true) {
                showError(
                    result.message ||
                    "ثبت درخواست انجام نشد. لطفاً دوباره تلاش کنید."
                );
                return;
            }

            form.hidden = true;
            successMessage.hidden = false;

        } catch (error) {
            console.error("Collaboration request error:", error);
            showError("ارتباط با سرور برقرار نشد.");
        } finally {
            setLoading(false);
        }
    });

    if (newRequestButton) {
        newRequestButton.addEventListener("click", () => {
            form.reset();
            form.hidden = false;
            successMessage.hidden = true;
            clearError();

            submitButton.disabled = false;
            submitButton.textContent = "ثبت درخواست همکاری";
        });
    }
});
