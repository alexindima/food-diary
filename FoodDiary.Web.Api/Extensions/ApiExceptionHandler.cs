using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Web.Api.Extensions;

public sealed class ApiExceptionHandler(
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler {
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken) {
        if (exception is CurrentUserUnavailableException) {
            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;

            var unauthorizedResponse = new ApiErrorHttpResponse(
                "Authentication.Unauthorized",
                "Authentication is required.",
                httpContext.TraceIdentifier);

            await httpContext.Response.WriteAsJsonAsync(unauthorizedResponse, cancellationToken);
            return true;
        }

        if (exception is DbUpdateConcurrencyException) {
            logger.LogWarning(exception, "Concurrency conflict while processing request {Method} {Path}.",
                httpContext.Request.Method,
                httpContext.Request.Path);

            httpContext.Response.StatusCode = StatusCodes.Status409Conflict;

            var conflictResponse = new ApiErrorHttpResponse(
                "Concurrency.Conflict",
                "The resource was modified by another request. Please retry.",
                httpContext.TraceIdentifier);

            await httpContext.Response.WriteAsJsonAsync(conflictResponse, cancellationToken);
            return true;
        }

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
