using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using System.Security.Claims;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Results;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Presentation.Api.Security;
using Microsoft.AspNetCore.Authorization;

namespace FoodDiary.Web.Api.Extensions;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class ImpersonationAccessGuardMiddleware(
    RequestDelegate next,
    ILogger<ImpersonationAccessGuardMiddleware> logger) {
    public async Task InvokeAsync(HttpContext context) {
        Endpoint? endpoint = context.GetEndpoint();
        if (!IsImpersonated(context.User) || !MustBlock(context.Request.Method, endpoint)) {
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

    private static bool MustBlock(string method, Endpoint? endpoint) {
        if (endpoint is null) {
            return false;
        }

        if (endpoint.Metadata.GetMetadata<BlockImpersonatedAccessAttribute>() is not null) {
            return true;
        }

        if (IsSafeMethod(method) ||
            endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null ||
            endpoint.Metadata.GetMetadata<AllowImpersonatedAccessAttribute>() is not null) {
            return false;
        }

        return endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Count > 0;
    }

    private static bool IsSafeMethod(string method) =>
        HttpMethods.IsGet(method) ||
        HttpMethods.IsHead(method) ||
        HttpMethods.IsOptions(method) ||
        HttpMethods.IsTrace(method);
}
