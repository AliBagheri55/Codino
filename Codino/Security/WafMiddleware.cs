using System.Text.RegularExpressions;

namespace Codino.Security;

public sealed class WafMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<WafMiddleware> _logger;
    private readonly IConfiguration _configuration;

    private static readonly Regex[] SuspiciousPatterns =
    {
        new(
            @"<\s*script\b|javascript\s*:|onerror\s*=|onload\s*=",
            RegexOptions.IgnoreCase | RegexOptions.Compiled),

        new(
            @"(?:\bunion\b\s+\bselect\b|\bselect\b.+\bfrom\b|\binsert\b\s+\binto\b|\bdrop\b\s+\btable\b|'\s*(?:or|and)\s+\d+\s*=\s*\d+|--\s|/\*)",
            RegexOptions.IgnoreCase |
            RegexOptions.Compiled |
            RegexOptions.Singleline)
    };

    public WafMiddleware(
        RequestDelegate next,
        ILogger<WafMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string path = context.Request.Path.Value ?? string.Empty;
        string query = context.Request.QueryString.Value ?? string.Empty;

        string target = path + query;

        string? reason = null;

        if (target.Length > 8192)
        {
            reason = "Request target exceeds 8192 characters";
        }
        else
        {
            foreach (Regex pattern in SuspiciousPatterns)
            {
                if (pattern.IsMatch(target))
                {
                    reason = "Suspicious request pattern detected";
                    break;
                }
            }
        }

        if (reason is not null)
        {
            // Never log the query string: it may contain sensitive values.
            _logger.LogWarning(
                "WAF event. Reason: {Reason}; Method: {Method}; Path: {Path}; TraceId: {TraceId}",
                reason,
                context.Request.Method,
                path,
                context.TraceIdentifier);

            string mode = _configuration["Waf:Mode"] ?? "Monitor";

            if (string.Equals(
                mode,
                "Block",
                StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode =
                    StatusCodes.Status403Forbidden;

                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Request blocked by security policy.",
                    traceId = context.TraceIdentifier
                });

                return;
            }
        }

        await _next(context);
    }
}
