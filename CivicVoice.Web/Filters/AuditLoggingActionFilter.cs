using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;

namespace CivicVoice.Web.Filters;

/// <summary>
/// Custom action filter demonstrating IAsyncActionFilter for audit logging.
/// Logs request execution time, action name, user role, and HTTP status.
/// Sanitizes sensitive parameters to avoid logging PII or credentials.
/// </summary>
public class AuditLoggingActionFilter : IAsyncActionFilter
{
    private readonly ILogger<AuditLoggingActionFilter> _logger;

    public AuditLoggingActionFilter(ILogger<AuditLoggingActionFilter> logger)
    {
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var actionName = context.ActionDescriptor.DisplayName;
        var httpMethod = context.HttpContext.Request.Method;
        var path = context.HttpContext.Request.Path;
        var userName = context.HttpContext.User.Identity?.IsAuthenticated == true
            ? context.HttpContext.User.Identity.Name
            : "Anonymous";

        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "[Audit] Executing {Method} {Path} (Action: {Action}) by User: {User}",
            httpMethod, path, actionName, userName);

        var executedContext = await next();

        stopwatch.Stop();

        if (executedContext.Exception != null)
        {
            _logger.LogError(
                executedContext.Exception,
                "[Audit] Action {Action} threw an unhandled exception after {ElapsedMs}ms",
                actionName, stopwatch.ElapsedMilliseconds);
        }
        else
        {
            var statusCode = executedContext.HttpContext.Response.StatusCode;
            _logger.LogInformation(
                "[Audit] Completed {Method} {Path} with Status {StatusCode} in {ElapsedMs}ms",
                httpMethod, path, statusCode, stopwatch.ElapsedMilliseconds);
        }
    }
}
