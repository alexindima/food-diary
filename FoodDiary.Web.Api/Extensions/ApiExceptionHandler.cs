using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Diagnostics;

namespace FoodDiary.Web.Api.Extensions;

public sealed class ApiExceptionHandler(
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler {
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken) {
        logger.LogError(exception, "Unhandled exception while processing request {Method} {Path}.",
            httpContext.Request.Method,
            httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var response = new ApiErrorHttpResponse(
            "Server.Unexpected",
            "An unexpected error occurred.",
            httpContext.TraceIdentifier);

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        return true;
    }
}
