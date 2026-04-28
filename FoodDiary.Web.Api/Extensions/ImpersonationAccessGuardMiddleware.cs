using System.Security.Claims;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Presentation.Api.Security;

namespace FoodDiary.Web.Api.Extensions;

public sealed class ImpersonationAccessGuardMiddleware(
    RequestDelegate next,
    ILogger<ImpersonationAccessGuardMiddleware> logger) {
    public async Task InvokeAsync(HttpContext context) {
        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<BlockImpersonatedAccessAttribute>() is null ||
            !IsImpersonated(context.User)) {
            await next(context);
            return;
        }

        logger.LogWarning(
            "Blocked impersonated request to protected action {Method} {Path}. ActorUserId={ActorUserId}, TargetUserId={TargetUserId}.",
            context.Request.Method,
            context.Request.Path,
            context.User.FindFirstValue(JwtImpersonationClaimNames.ActorUserId),
            context.User.FindFirstValue(ClaimTypes.NameIdentifier));

        var error = Errors.Authentication.ImpersonationActionForbidden;
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(
            new ApiErrorHttpResponse(error.Code, error.Message, context.TraceIdentifier),
            context.RequestAborted);
    }

    private static bool IsImpersonated(ClaimsPrincipal user) =>
        user.HasClaim(claim =>
            string.Equals(claim.Type, JwtImpersonationClaimNames.IsImpersonation, StringComparison.Ordinal) &&
            string.Equals(claim.Value, "true", StringComparison.OrdinalIgnoreCase));
}
