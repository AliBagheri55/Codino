using Codino.Models.Code;

namespace Codino.Services;

public class DockerCodeExecutionService : ICodeExecutionService
{
    private readonly IConfiguration _configuration;

    public DockerCodeExecutionService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<CodeExecutionResponse> ExecuteAsync(CodeExecutionRequest request,CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        return new CodeExecutionResponse
        {
            Success = false,
            Output = string.Empty,
            Error = "سرویس اجرای کد هنوز پیکربندی نشده است.",
            ExecutionTimeMs = 0
        };
    }
}