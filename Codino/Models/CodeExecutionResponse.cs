namespace Codino.Models.Code
{
    public class CodeExecutionResponse
    {
        public bool Success { get; set; }

        public string Output { get; set; } = string.Empty;

        public string Error { get; set; } = string.Empty;

        public long ExecutionTimeMs { get; set; }
    }
}