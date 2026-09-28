using Codino.Models.Code;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text;

namespace Codino.Controllers
{
    [ApiController]
    [Route("api/code")]
    public class CodeController : ControllerBase
    {
        private static readonly HashSet<string> AllowedLanguages =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "csharp",
                "cpp",
                "c",
                "python"
            };


        [HttpPost("execute")]
        [RequestSizeLimit(32768)]
        public async Task<IActionResult> Execute(
            [FromBody] CodeExecutionRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                return BadRequest(
                    new CodeExecutionResponse
                    {
                        Success = false,
                        Error = "درخواست نامعتبر است."
                    });
            }


            if (string.IsNullOrWhiteSpace(request.Language))
            {
                return BadRequest(
                    new CodeExecutionResponse
                    {
                        Success = false,
                        Error = "زبان برنامه‌نویسی مشخص نشده است."
                    });
            }


            if (string.IsNullOrWhiteSpace(request.Code))
            {
                return BadRequest(
                    new CodeExecutionResponse
                    {
                        Success = false,
                        Error = "کد خالی است."
                    });
            }


            if (!AllowedLanguages.Contains(
                    request.Language))
            {
                return BadRequest(
                    new CodeExecutionResponse
                    {
                        Success = false,
                        Error = "این زبان پشتیبانی نمی‌شود."
                    });
            }


            if (request.Code.Length > 10000)
            {
                return BadRequest(
                    new CodeExecutionResponse
                    {
                        Success = false,
                        Error =
                            "حجم کد بیشتر از حد مجاز است."
                    });
            }


            var stopwatch =
                Stopwatch.StartNew();


            try
            {
                CodeExecutionResponse result =
                    await ExecuteDockerAsync(
                        request.Language.ToLowerInvariant(),
                        request.Code,
                        cancellationToken);


                stopwatch.Stop();


                result.ExecutionTimeMs =
                    stopwatch.ElapsedMilliseconds;


                return Ok(result);
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();


                return Ok(
                    new CodeExecutionResponse
                    {
                        Success = false,
                        Error =
                            "اجرای برنامه لغو شد یا زمان اجرای آن تمام شد.",
                        ExecutionTimeMs =
                            stopwatch.ElapsedMilliseconds
                    });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();


                return StatusCode(
                    500,
                    new CodeExecutionResponse
                    {
                        Success = false,
                        Error =
                            "خطا در اجرای برنامه: " +
                            ex.Message,
                        ExecutionTimeMs =
                            stopwatch.ElapsedMilliseconds
                    });
            }
        }


        private async Task<CodeExecutionResponse>
            ExecuteDockerAsync(
                string language,
                string code,
                CancellationToken cancellationToken)
        {
            string image;

            string fileName;

            string command;


            switch (language)
            {
                case "python":

                    image =
                        "python:3.13-slim";

                    fileName =
                        "main.py";

                    command =
                        "python /app/main.py";

                    break;


                case "cpp":

                    image =
                        "gcc:15";

                    fileName =
                        "main.cpp";

                    command =
                        "g++ /app/main.cpp " +
                        "-O2 " +
                        "-std=c++17 " +
                        "-o /tmp/program " +
                        "&& /tmp/program";

                    break;


                case "c":

                    image =
                        "gcc:15";

                    fileName =
                        "main.c";

                    command =
                        "gcc /app/main.c " +
                        "-O2 " +
                        "-std=c17 " +
                        "-o /tmp/program " +
                        "&& /tmp/program";

                    break;


                case "csharp":

                    image =
                        "mcr.microsoft.com/dotnet/sdk:10.0";

                    fileName =
                        "Program.cs";

                    command =
                        "cd /tmp " +
                        "&& mkdir -p codino " +
                        "&& cp /app/Program.cs /tmp/codino/Program.cs " +
                        "&& cd /tmp/codino " +
                        "&& printf '%s' " +
                        "'<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup></Project>' " +
                        "> Codino.csproj " +
                        "&& dotnet run " +
                        "--project Codino.csproj " +
                        "--no-restore";

                    break;


                default:

                    throw new InvalidOperationException(
                        "زبان پشتیبانی نمی‌شود.");
            }


            string encodedCode =
                Convert.ToBase64String(
                    Encoding.UTF8.GetBytes(code));


            string containerName =
                "codino-" +
                Guid.NewGuid().ToString("N");


            List<string> arguments =
                new()
                {
                    "run",

                    "--rm",

                    "--name",
                    containerName,

                    "--network",
                    "none",

                    "--memory",
                    "128m",

                    "--cpus",
                    "0.5",

                    "--pids-limit",
                    "64",

                    "--read-only",

                    "--cap-drop",
                    "ALL",

                    "--security-opt",
                    "no-new-privileges",

                    "--tmpfs",
                    "/tmp:rw,nosuid,nodev,size=64m",

                    image,

                    "sh",

                    "-c",

                    $"mkdir -p /app && " +
                    $"echo '{encodedCode}' | base64 -d > /app/{fileName} && " +
                    command
                };


            ProcessStartInfo startInfo =
                new()
                {
                    FileName = "docker",

                    UseShellExecute = false,

                    CreateNoWindow = true,

                    RedirectStandardOutput = true,

                    RedirectStandardError = true
                };


            foreach (string argument in arguments)
            {
                startInfo.ArgumentList.Add(
                    argument);
            }


            using Process process =
                new();

            process.StartInfo =
                startInfo;


            if (!process.Start())
            {
                throw new Exception(
                    "Docker اجرا نشد.");
            }


            Task<string> outputTask =
                ReadLimitedAsync(
                    process.StandardOutput,
                    10000,
                    cancellationToken);


            Task<string> errorTask =
                ReadLimitedAsync(
                    process.StandardError,
                    10000,
                    cancellationToken);


            using CancellationTokenSource timeout =
                new(TimeSpan.FromSeconds(10));


            using CancellationTokenSource linked =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    timeout.Token);


            try
            {
                await process.WaitForExitAsync(
                    linked.Token);
            }
            catch
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(true);
                    }
                }
                catch
                {
                }


                await RemoveDockerContainerAsync(
                    containerName);


                throw;
            }


            string output =
                await outputTask;


            string error =
                await errorTask;


            bool success =
                process.ExitCode == 0;


            return new CodeExecutionResponse
            {
                Success = success,

                Output = output,

                Error = success
                    ? string.Empty
                    : error
            };
        }


        private static async Task<string>
            ReadLimitedAsync(
                StreamReader reader,
                int maximumCharacters,
                CancellationToken cancellationToken)
        {
            StringBuilder result =
                new();


            char[] buffer =
                new char[4096];


            while (true)
            {
                int count =
                    await reader.ReadAsync(
                        buffer,
                        cancellationToken);


                if (count == 0)
                {
                    break;
                }


                int remaining =
                    maximumCharacters -
                    result.Length;


                if (remaining > 0)
                {
                    result.Append(
                        buffer,
                        0,
                        Math.Min(
                            count,
                            remaining));
                }
            }


            if (result.Length >= maximumCharacters)
            {
                result.Append(
                    "\n\n[خروجی بیش از حد مجاز بود]");
            }


            return result.ToString();
        }


        private static async Task
            RemoveDockerContainerAsync(
                string containerName)
        {
            try
            {
                ProcessStartInfo info =
                    new()
                    {
                        FileName = "docker",

                        UseShellExecute = false,

                        CreateNoWindow = true
                    };


                info.ArgumentList.Add(
                    "rm");

                info.ArgumentList.Add(
                    "-f");

                info.ArgumentList.Add(
                    containerName);


                using Process process =
                    Process.Start(info)!;


                await process.WaitForExitAsync();
            }
            catch
            {
            }
        }
    }
}