/* =========================================================
   CODINO - SITE.JS
   ========================================================= */


/* =========================================================
   MOBILE MENU
   ========================================================= */

const mobileMenuButton =
    document.getElementById("mobileMenuButton");

const navMenu =
    document.getElementById("navMenu");


if (mobileMenuButton && navMenu) {

    mobileMenuButton.addEventListener(
        "click",
        () => {

            navMenu.classList.toggle("active");

        }
    );


    const navLinks =
        navMenu.querySelectorAll("a");


    navLinks.forEach(link => {

        link.addEventListener(
            "click",
            () => {

                navMenu.classList.remove(
                    "active"
                );

            }
        );

    });

}


/* =========================================================
   CODE PLAYGROUND ELEMENTS
   ========================================================= */

const codeEditor =
    document.getElementById("codeEditor");

const codeOutput =
    document.getElementById("codeOutput");

const runCode =
    document.getElementById("runCode");

const clearCode =
    document.getElementById("clearCode");

const languageSelect =
    document.getElementById("languageSelect");

const currentLanguage =
    document.getElementById("currentLanguage");

const executionStatus =
    document.getElementById("executionStatus");


/* =========================================================
   LANGUAGE NAMES
   ========================================================= */

const languageNames = {

    html: "HTML + CSS",

    javascript: "JavaScript",

    csharp: "C#",

    cpp: "C++",

    c: "C",

    python: "Python"

};


/* =========================================================
   DEFAULT CODES
   ========================================================= */

const defaultCodes = {

    /* =====================================================
       HTML + CSS
    ===================================================== */

    html: `<!DOCTYPE html>
<html lang="fa">

<head>

    <meta charset="UTF-8">

    <title>
        Codino
    </title>

    <style>

        body {

            background: #101827;

            color: white;

            font-family: Arial, sans-serif;

            text-align: center;

            padding: 50px;

        }

        h1 {

            color: #7c7bff;

        }

        p {

            color: #b8c0d0;

            font-size: 18px;

        }

        button {

            padding: 12px 24px;

            border: none;

            border-radius: 8px;

            background: #5d5cff;

            color: white;

            cursor: pointer;

        }

        button:hover {

            background: #7776ff;

        }

    </style>

</head>


<body>

    <h1>
        سلام Codino 👋
    </h1>

    <p>
        این کد HTML + CSS من است.
    </p>

    <button onclick="hello()">
        کلیک کن
    </button>


    <script>

        function hello() {

            alert(
                "HTML + CSS + JavaScript اجرا شد 🚀"
            );

        }

    <\/script>

</body>

</html>`,



    /* =====================================================
       JAVASCRIPT
    ===================================================== */

    javascript: `// Codino JavaScript Playground

const name = "Ali";

console.log("Hello " + name);

document.body.innerHTML = \`
    <div style="
        font-family: Arial;
        text-align: center;
        padding: 50px;
        background: #101827;
        color: white;
        min-height: 100vh;
    ">

        <h1 style="color:#7c7bff;">
            JavaScript اجرا شد 🚀
        </h1>

        <p>
            سلام \${name}
        </p>

        <button
            onclick="showMessage()"
            style="
                padding:12px 20px;
                border:none;
                border-radius:8px;
                background:#5d5cff;
                color:white;
                cursor:pointer;
            "
        >
            کلیک کن
        </button>

    </div>
\`;

function showMessage() {

    alert(
        "کد JavaScript با موفقیت اجرا شد!"
    );

}`,



    /* =====================================================
       C#
    ===================================================== */

    csharp: `using System;

class Program
{
    static void Main()
    {
        Console.WriteLine("Hello from Codino!");

        int a = 10;
        int b = 20;

        int result = a + b;

        Console.WriteLine(
            "Result = " + result
        );
    }
}`,



    /* =====================================================
       C++
    ===================================================== */

    cpp: `#include <iostream>

using namespace std;

int main()
{
    cout << "Hello from Codino!" << endl;

    int a = 10;
    int b = 20;

    int result = a + b;

    cout << "Result = "
         << result
         << endl;

    return 0;
}`,



    /* =====================================================
       C
    ===================================================== */

    c: `#include <stdio.h>

int main()
{
    printf("Hello from Codino!\\n");

    int a = 10;
    int b = 20;

    int result = a + b;

    printf(
        "Result = %d\\n",
        result
    );

    return 0;
}`,



    /* =====================================================
       PYTHON
    ===================================================== */

    python: `print("Hello from Codino!")

a = 10
b = 20

result = a + b

print("Result =", result)

for i in range(5):
    print("Number:", i)`

};


/* =========================================================
   LOAD LANGUAGE
   ========================================================= */

function loadLanguage(language) {

    if (!codeEditor) {
        return;
    }


    const code =
        defaultCodes[language] || "";


    codeEditor.value =
        code;


    if (currentLanguage) {

        currentLanguage.textContent =
            languageNames[language] ||
            language;

    }


    if (executionStatus) {

        executionStatus.textContent =
            "آماده اجرا";

        executionStatus.className =
            "execution-status";

    }


    if (codeOutput) {

        codeOutput.innerHTML = `

            <div class="empty-output">

                کد خود را بنویسید
                و روی اجرای کد کلیک کنید.

            </div>

        `;

    }

}


/* =========================================================
   INITIAL LANGUAGE
   ========================================================= */

if (languageSelect) {

    loadLanguage(
        languageSelect.value
    );


    languageSelect.addEventListener(
        "change",
        () => {

            const language =
                languageSelect.value;

            loadLanguage(
                language
            );

        }
    );

}


/* =========================================================
   CREATE HTML PREVIEW
   ========================================================= */

function runHtmlCode(code) {

    if (!codeOutput) {
        return;
    }


    codeOutput.innerHTML = "";


    const iframe =
        document.createElement("iframe");


    iframe.className =
        "code-preview";


    iframe.title =
        "Codino HTML Preview";


    iframe.setAttribute(
        "sandbox",
        "allow-scripts"
    );


    iframe.setAttribute(
        "referrerpolicy",
        "no-referrer"
    );


    codeOutput.appendChild(
        iframe
    );


    /*
     * از contentDocument استفاده نمی‌کنیم.
     *
     * srcdoc باعث می‌شود کد داخل iframe
     * به‌صورت ایزوله اجرا شود.
     */

    iframe.srcdoc =
        code;

}


/* =========================================================
   RUN JAVASCRIPT
   ========================================================= */

function runJavaScriptCode(code) {

    if (!codeOutput) {
        return;
    }


    const safeCode =
        JSON.stringify(code);


    const html = `

<!DOCTYPE html>

<html>

<head>

    <meta charset="UTF-8">

</head>


<body>

    <div
        id="console"
        style="
            font-family:Consolas,monospace;
            padding:20px;
            background:#101827;
            color:#ffffff;
            min-height:100vh;
        "
    >

        <div
            style="
                color:#7c7bff;
                margin-bottom:15px;
                font-size:18px;
            "
        >

            JavaScript Console

        </div>

    </div>


<script>

const consoleBox =
    document.getElementById("console");


const originalLog =
    console.log;


console.log = function(...args) {

    originalLog(...args);


    const line =
        document.createElement("div");


    line.style.marginBottom =
        "8px";


    line.style.color =
        "#65e69a";


    line.textContent =
        args
            .map(item => {

                if (
                    typeof item ===
                    "object"
                ) {

                    try {

                        return JSON.stringify(
                            item
                        );

                    }
                    catch {

                        return String(
                            item
                        );

                    }

                }

                return String(item);

            })
            .join(" ");


    consoleBox.appendChild(
        line
    );

};


window.onerror =
    function(
        message,
        source,
        line,
        column
    ) {

        const error =
            document.createElement("div");


        error.style.color =
            "#ff7773";


        error.style.marginBottom =
            "10px";


        error.textContent =
            "Error: " +
            message;


        consoleBox.appendChild(
            error
        );

    };


try {

    const userCode =
        ${safeCode};

    eval(userCode);

}
catch(error) {

    const errorElement =
        document.createElement("div");


    errorElement.style.color =
        "#ff7773";


    errorElement.style.marginTop =
        "10px";


    errorElement.textContent =
        "Error: " +
        error.message;


    consoleBox.appendChild(
        errorElement
    );

}

<\/script>

</body>

</html>
`;


    runHtmlCode(
        html
    );

}


/* =========================================================
   REAL BACKEND CODE EXECUTION
   ========================================================= */

async function executeBackendCode(
    language,
    code
) {

    if (!codeOutput) {
        return;
    }


    if (executionStatus) {

        executionStatus.textContent =
            "در حال اجرا...";

        executionStatus.className =
            "execution-status running";

    }


    codeOutput.innerHTML = `

        <div class="empty-output">

            در حال اجرای کد...

        </div>

    `;


    try {

        const response =
            await fetch(
                "/api/code/execute",
                {
                    method: "POST",

                    headers: {
                        "Content-Type":
                            "application/json"
                    },

                    body: JSON.stringify({
                        language: language,
                        code: code
                    })
                }
            );


        let result;


        try {

            result =
                await response.json();

        }
        catch {

            throw new Error(
                "پاسخ سرور قابل خواندن نیست."
            );

        }


        if (!response.ok) {

            throw new Error(
                result.error ||
                result.message ||
                "خطا در ارتباط با سرور."
            );

        }


        if (result.success) {

            const output =
                result.output ||
                "برنامه اجرا شد اما خروجی نداشت.";


            codeOutput.innerHTML = `

                <pre class="backend-output success-output">${escapeHtml(
                output
            )}</pre>

            `;


            if (executionStatus) {

                executionStatus.textContent =
                    `اجرا شد - ${result.executionTimeMs ?? 0}ms`;

                executionStatus.className =
                    "execution-status success";

            }

        }
        else {

            const error =
                result.error ||
                "خطای نامشخص در اجرای برنامه.";


            codeOutput.innerHTML = `

                <pre class="backend-output error-output">${escapeHtml(
                error
            )}</pre>

            `;


            if (executionStatus) {

                executionStatus.textContent =
                    "خطا در اجرا";

                executionStatus.className =
                    "execution-status error";

            }

        }

    }
    catch (error) {

        codeOutput.innerHTML = `

            <div class="code-message">

                <h3>
                    خطا در اجرای کد
                </h3>

                <p>
                    ${escapeHtml(
            error.message ||
            "خطای نامشخص"
        )}
                </p>

            </div>

        `;


        if (executionStatus) {

            executionStatus.textContent =
                "خطای سرور";

            executionStatus.className =
                "execution-status error";

        }

    }

}


/* =========================================================
   ESCAPE HTML
   ========================================================= */

function escapeHtml(text) {

    if (text === null ||
        text === undefined) {

        return "";

    }


    return String(text)

        .replaceAll(
            "&",
            "&amp;"
        )

        .replaceAll(
            "<",
            "&lt;"
        )

        .replaceAll(
            ">",
            "&gt;"
        )

        .replaceAll(
            '"',
            "&quot;"
        )

        .replaceAll(
            "'",
            "&#039;"
        );

}


/* =========================================================
   RUN CODE
   ========================================================= */

if (
    runCode &&
    codeEditor &&
    codeOutput
) {

    runCode.addEventListener(
        "click",
        async () => {

            const code =
                codeEditor.value.trim();


            const language =
                languageSelect
                    ? languageSelect.value
                    : "html";


            /* =========================
               EMPTY CODE
            ========================== */

            if (!code) {

                codeOutput.innerHTML = `

                    <div class="empty-output">

                        لطفاً ابتدا کد خود را وارد کنید.

                    </div>

                `;


                if (executionStatus) {

                    executionStatus.textContent =
                        "کدی وجود ندارد";

                    executionStatus.className =
                        "execution-status error";

                }


                return;

            }


            /* =========================
               RUNNING
            ========================== */

            if (executionStatus) {

                executionStatus.textContent =
                    "در حال اجرا...";

                executionStatus.className =
                    "execution-status running";

            }


            /* =========================
               HTML
            ========================== */

            if (language === "html") {

                runHtmlCode(
                    code
                );


                if (executionStatus) {

                    executionStatus.textContent =
                        "اجرا شد";

                    executionStatus.className =
                        "execution-status success";

                }


                return;

            }


            /* =========================
               JAVASCRIPT
            ========================== */

            if (language === "javascript") {

                runJavaScriptCode(
                    code
                );


                if (executionStatus) {

                    executionStatus.textContent =
                        "اجرا شد";

                    executionStatus.className =
                        "execution-status success";

                }


                return;

            }


            /* =========================
               BACKEND LANGUAGES
            ========================== */

            if (
                language === "csharp" ||
                language === "cpp" ||
                language === "c" ||
                language === "python"
            ) {

                await executeBackendCode(
                    language,
                    code
                );


                return;

            }

        }
    );

}


/* =========================================================
   CLEAR CODE
   ========================================================= */

if (
    clearCode &&
    codeEditor &&
    codeOutput
) {

    clearCode.addEventListener(
        "click",
        () => {

            codeEditor.value = "";


            codeOutput.innerHTML = `

                <div class="empty-output">

                    منتظر اجرای کد...

                </div>

            `;


            if (executionStatus) {

                executionStatus.textContent =
                    "آماده اجرا";

                executionStatus.className =
                    "execution-status";

            }

        }
    );

}


/* =========================================================
   TAB SUPPORT
   ========================================================= */

if (codeEditor) {

    codeEditor.addEventListener(
        "keydown",
        event => {

            if (event.key !== "Tab") {
                return;
            }


            event.preventDefault();


            const start =
                codeEditor.selectionStart;


            const end =
                codeEditor.selectionEnd;


            const before =
                codeEditor.value.substring(
                    0,
                    start
                );


            const after =
                codeEditor.value.substring(
                    end
                );


            codeEditor.value =
                before +
                "    " +
                after;


            codeEditor.selectionStart =
                start + 4;


            codeEditor.selectionEnd =
                start + 4;

        }
    );

}


/* =========================================================
   COMMENT RATING
   ========================================================= */

const ratingStars =
    document.getElementById(
        "ratingStars"
    );

const ratingInput =
    document.getElementById(
        "rating"
    );


if (
    ratingStars &&
    ratingInput
) {

    const stars =
        ratingStars.querySelectorAll(
            "button"
        );


    stars.forEach(star => {

        star.addEventListener(
            "click",
            () => {

                const selectedRating =
                    Number(
                        star.dataset.rating
                    );


                ratingInput.value =
                    selectedRating;


                stars.forEach(item => {

                    const rating =
                        Number(
                            item.dataset.rating
                        );


                    if (
                        rating <=
                        selectedRating
                    ) {

                        item.classList.add(
                            "active"
                        );

                    }
                    else {

                        item.classList.remove(
                            "active"
                        );

                    }

                });

            }
        );

    });

}


/* =========================================================
   COMMENT COUNTER
   ========================================================= */

const commentText =
    document.getElementById(
        "commentText"
    );

const commentCounter =
    document.getElementById(
        "commentCounter"
    );


if (
    commentText &&
    commentCounter
) {

    const updateCounter =
        () => {

            commentCounter.textContent =
                commentText.value.length;

        };


    commentText.addEventListener(
        "input",
        updateCounter
    );


    updateCounter();

}


/* =========================================================
   COMMENT FORM VALIDATION
   ========================================================= */

const commentForm =
    document.getElementById(
        "commentForm"
    );


if (commentForm) {

    commentForm.addEventListener(
        "submit",
        event => {

            const rating =
                Number(
                    document.getElementById(
                        "rating"
                    )?.value || 0
                );


            const text =
                document.getElementById(
                    "commentText"
                )?.value.trim() || "";


            if (
                rating < 1 ||
                rating > 5
            ) {

                event.preventDefault();


                alert(
                    "لطفاً ابتدا امتیاز خود را انتخاب کنید."
                );


                return;

            }


            if (!text) {

                event.preventDefault();


                alert(
                    "لطفاً متن نظر خود را وارد کنید."
                );


                return;

            }


            if (text.length > 500) {

                event.preventDefault();


                alert(
                    "نظر نمی‌تواند بیشتر از ۵۰۰ کاراکتر باشد."
                );


                return;

            }

        }
    );

}


/* =========================================================
   NAVBAR SCROLL
   ========================================================= */

const navbar =
    document.querySelector(
        ".navbar"
    );


window.addEventListener(
    "scroll",
    () => {

        if (!navbar) {
            return;
        }


        if (window.scrollY > 30) {

            navbar.style.boxShadow =
                "0 10px 35px rgba(0, 0, 0, 0.25)";

        }
        else {

            navbar.style.boxShadow =
                "none";

        }

    }
);
/* =========================================================
   COURSE LOGIN CHECK
   ========================================================= */

const courseLoginButtons =
    document.querySelectorAll(
        ".course-login-required"
    );


if (courseLoginButtons.length > 0) {

    courseLoginButtons.forEach(
        button => {

            button.addEventListener(
                "click",
                event => {

                    event.preventDefault();

                    alert(
                        "شما هنوز ثبت نام نکرده اید."
                    );

                }
            );

        }
    );

}