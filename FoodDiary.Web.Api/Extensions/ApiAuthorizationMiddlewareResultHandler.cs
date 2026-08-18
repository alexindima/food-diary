using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace FoodDiary.Web.Api.Extensions;

public sealed class ApiAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler {
    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult) {
        if (authorizeResult.Succeeded) {
            await next(context).ConfigureAwait(false);
            return;
        }

        if (authorizeResult.Forbidden) {
            await context.ForbidAsync().ConfigureAwait(false);
            await WriteErrorAsync(
                context,
                StatusCodes.Status403Forbidden,
                "Authentication.Forbidden",
                "You do not have permission to access this resource.",
                context.RequestAborted).ConfigureAwait(false);
            return;
        }

        await context.ChallengeAsync().ConfigureAwait(false);
        await WriteErrorAsync(
            context,
            StatusCodes.Status401Unauthorized,
            "Authentication.Unauthorized",
            "Authentication is required.",
            context.RequestAborted).ConfigureAwait(false);
    }

    private static async Task WriteErrorAsync(
        HttpContext context,
        int statusCode,
        string error,
        string message,
        CancellationToken cancellationToken) {
        if (context.Response.HasStarted) {
            return;
        }

        context.Response.StatusCode = statusCode;
        var response = new ApiErrorHttpResponse(error, message, context.TraceIdentifier);
        await context.Response.WriteAsJsonAsync(response, cancellationToken).ConfigureAwait(false);
    }
}
