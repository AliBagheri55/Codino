
document.addEventListener("DOMContentLoaded", function () {

    // دریافت عناصر فرم ثبت پروژه
    const form = document.getElementById("projectRequestForm");
    const openButton = document.getElementById("openProjectConfirm");
    const modal = document.getElementById("projectConfirmModal");
    const confirmButton = document.getElementById("confirmProjectSubmit");
    const cancelButton = document.getElementById("cancelProjectSubmit");

    // بررسی وجود عناصر موردنیاز
    if (!form || !openButton || !modal || !confirmButton || !cancelButton) {
        console.error("عناصر فرم ثبت پروژه پیدا نشدند.");
        return;
    }

    // در ابتدا پنجره تأیید مخفی باشد
    modal.hidden = true;

    // باز کردن پنجره تأیید
    function openModal() {

        // بررسی تکمیل صحیح فیلدهای فرم
        if (!form.checkValidity()) {
            form.reportValidity();
            return;
        }

        modal.hidden = false;

        // جلوگیری از اسکرول صفحه
        document.body.style.overflow = "hidden";

        confirmButton.focus();
    }

    // بستن پنجره تأیید
    function closeModal() {

        modal.hidden = true;

        // فعال کردن مجدد اسکرول صفحه
        document.body.style.overflow = "";

        openButton.focus();
    }

    // کلیک روی دکمه ثبت درخواست پروژه
    openButton.addEventListener("click", function () {
        openModal();
    });

    // کلیک روی دکمه لغو
    cancelButton.addEventListener("click", function () {
        closeModal();
    });

    // کلیک روی دکمه تأیید نهایی
    confirmButton.addEventListener("click", function () {

        // بررسی مجدد اعتبار فرم
        if (!form.checkValidity()) {
            closeModal();
            form.reportValidity();
            return;
        }

        // جلوگیری از ثبت چندباره
        confirmButton.disabled = true;
        confirmButton.textContent = "در حال ارسال درخواست...";

        openButton.disabled = true;

        // ارسال فرم به کنترلر
        form.requestSubmit();
    });

    // بستن پنجره با کلید Escape
    document.addEventListener("keydown", function (event) {

        if (event.key === "Escape" && !modal.hidden) {
            closeModal();
        }

    });

    // بستن پنجره با کلیک روی فضای بیرونی
    modal.addEventListener("click", function (event) {

        if (event.target === modal) {
            closeModal();
        }

    });

    // مدیریت ارسال فرم
    form.addEventListener("submit", function (event) {

        if (!form.checkValidity()) {
            event.preventDefault();

            confirmButton.disabled = false;
            confirmButton.textContent = "بله، ثبت درخواست";

            openButton.disabled = false;

            form.reportValidity();
            return;
        }

        // جلوگیری از ارسال مجدد
        confirmButton.disabled = true;
        openButton.disabled = true;

    });

});
