using Codino.Models.Code;

namespace Codino.Services;

public interface ICodeExecutionService
{
    Task<CodeExecutionResponse> ExecuteAsync(
        CodeExecutionRequest request,
        CancellationToken cancellationToken = default);
}