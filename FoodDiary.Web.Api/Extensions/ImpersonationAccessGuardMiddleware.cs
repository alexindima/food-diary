using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using System.Security.Claims;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Results;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Presentation.Api.Security;

namespace FoodDiary.Web.Api.Extensions;

public sealed class ImpersonationAccessGuardMiddleware(
    RequestDelegate next,
    ILogger<ImpersonationAccessGuardMiddleware> logger) {
    public async Task InvokeAsync(HttpContext context) {
        Endpoint? endpoint = context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<BlockImpersonatedAccessAttribute>() is null ||
            !IsImpersonated(context.User)) {
            await next(context).ConfigureAwait(false);
            return;
        }

        string routeLabel = TelemetryPrivacyProcessor.ResolveRouteLabel(context);
        logger.LogWarning(
            "Blocked impersonated request to protected action {Method} {Route}. TraceId={TraceId}.",
            context.Request.Method,
            routeLabel,
            context.TraceIdentifier);

        Error error = Errors.Authentication.ImpersonationActionForbidden;
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(
            new ApiErrorHttpResponse(error.Code, error.Message, context.TraceIdentifier),
            context.RequestAborted).ConfigureAwait(false);
    }

    private static bool IsImpersonated(ClaimsPrincipal user) =>
        user.HasClaim(claim =>
            string.Equals(claim.Type, JwtImpersonationClaimNames.IsImpersonation, StringComparison.Ordinal) &&
            string.Equals(claim.Value, "true", StringComparison.OrdinalIgnoreCase));
}
