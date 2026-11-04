using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace API;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        if (ex is OperationCanceledException) return true;

        var (status, title) = ex is ArgumentException
            ? (StatusCodes.Status400BadRequest, "Invalid request")
            : (StatusCodes.Status500InternalServerError, "Unexpected error");

        if (status == 500)
            logger.LogError(ex, "Unhandled exception on {Method} {Path}", ctx.Request.Method, ctx.Request.Path);

        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = status == 500 ? "An unexpected error occurred." : ex.Message
        }, ct);
        return true;
    }
}