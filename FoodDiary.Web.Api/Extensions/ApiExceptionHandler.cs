using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Web.Api.Extensions;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class ApiExceptionHandler(
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler {
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken) {
        switch (exception) {
            case OperationCanceledException when httpContext.RequestAborted.IsCancellationRequested:
                return HandleClientCancellation(httpContext);
            case BadHttpRequestException { StatusCode: StatusCodes.Status413PayloadTooLarge }: {
                    httpContext.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;

                    var payloadTooLargeResponse = new ApiErrorHttpResponse(
                        "Request.PayloadTooLarge",
                        "The request payload is too large.",
                        httpContext.TraceIdentifier);

                    await httpContext.Response.WriteAsJsonAsync(payloadTooLargeResponse, cancellationToken).ConfigureAwait(false);
                    return true;
                }
            case CurrentUserUnavailableException: {
                    httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;

                    var unauthorizedResponse = new ApiErrorHttpResponse(
                        "Authentication.Unauthorized",
                        "Authentication is required.",
                        httpContext.TraceIdentifier);

                    await httpContext.Response.WriteAsJsonAsync(unauthorizedResponse, cancellationToken).ConfigureAwait(false);
                    return true;
                }
            case DbUpdateConcurrencyException: {
                    logger.LogWarning(exception, "Concurrency conflict while processing request {Method} {Path}.",
                        httpContext.Request.Method,
                        TelemetryPrivacyProcessor.ResolveRouteLabel(httpContext));

                    httpContext.Response.StatusCode = StatusCodes.Status409Conflict;

                    var conflictResponse = new ApiErrorHttpResponse(
                        "Concurrency.Conflict",
                        "The resource was modified by another request. Please retry.",
                        httpContext.TraceIdentifier);

                    await httpContext.Response.WriteAsJsonAsync(conflictResponse, cancellationToken).ConfigureAwait(false);
                    return true;
                }
        }

        RequestObservabilityMiddleware.ReportHandledException(httpContext, exception);
        logger.LogError(exception, "Unhandled exception while processing request {Method} {Path}.",
            httpContext.Request.Method,
            TelemetryPrivacyProcessor.ResolveRouteLabel(httpContext));

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var response = new ApiErrorHttpResponse(
            "Server.Unexpected",
            "An unexpected error occurred.",
            httpContext.TraceIdentifier);

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken).ConfigureAwait(false);
        return true;
    }

    private bool HandleClientCancellation(HttpContext httpContext) {
        if (!httpContext.Response.HasStarted) {
            httpContext.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
        }

        logger.LogDebug(
            "Request {Method} {Path} was cancelled by the client.",
            httpContext.Request.Method,
            TelemetryPrivacyProcessor.ResolveRouteLabel(httpContext));
        return true;
    }

}
