using System.Diagnostics.CodeAnalysis;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using FoodDiary.Modules.Identity.Contracts.Errors;

namespace FoodDiary.Web.Api.Extensions;

[ExcludeFromCodeCoverage]
public sealed class ApiAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler {
    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult) {
        if (authorizeResult.Succeeded) {
            await next(context);
            return;
        }

        if (authorizeResult.Forbidden) {
            await context.ForbidAsync();
            bool confirmationRequired = authorizeResult.AuthorizationFailure?.FailureReasons
                .Any(reason => string.Equals(reason.Message, IdentityErrors.ReauthenticationRequired.Code, StringComparison.Ordinal)) == true;
            await WriteErrorAsync(
                context,
                StatusCodes.Status403Forbidden,
                confirmationRequired ? IdentityErrors.ReauthenticationRequired.Code : "Authentication.Forbidden",
                confirmationRequired ? IdentityErrors.ReauthenticationRequired.Message : "You do not have permission to access this resource.",
                context.RequestAborted);
            return;
        }

        await context.ChallengeAsync();
        await WriteErrorAsync(
            context,
            StatusCodes.Status401Unauthorized,
            "Authentication.Unauthorized",
            "Authentication is required.",
            context.RequestAborted);
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
        await context.Response.WriteAsJsonAsync(response, cancellationToken);
    }
}
