using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Presentation.Api.Features.Auth.Mappings;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Presentation.Api.Security;
using FoodDiary.Presentation.Api.Features.Auth.Requests;
using FoodDiary.Presentation.Api.Features.Auth.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Presentation.Api.Features.Auth;

[ApiController]
[Route("api/v{version:apiVersion}/auth")]
[RequestSizeLimit(AuthRequestLimits.MaxPayloadBytes)]
[RejectOversizedRequest(AuthRequestLimits.MaxPayloadBytes)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AuthSessionLifecycleController(ISender mediator) : BaseApiController(mediator) {
    [Authorize]
    [BlockImpersonatedAccess]
    [HttpGet("sessions")]
    [ProducesResponseType<IReadOnlyList<ActiveSessionHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status401Unauthorized)]
    public Task<IActionResult> GetSessions(
        [FromCurrentUser] Guid userId,
        [FromCurrentRefreshSession] Guid currentSessionId) =>
        HandleOk(
            userId.ToGetActiveSessionsQuery(currentSessionId),
            static sessions => sessions.Select(static session => session.ToHttpResponse()).ToArray());

    [Authorize]
    [BlockImpersonatedAccess]
    [HttpDelete("sessions/{sessionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status401Unauthorized)]
    public Task<IActionResult> RevokeSession(
        [FromCurrentUser] Guid userId,
        [FromCurrentRefreshSession] Guid currentSessionId,
        Guid sessionId) =>
        HandleNoContent(userId.ToRevokeSessionCommand(currentSessionId, sessionId));

    [Authorize]
    [BlockImpersonatedAccess]
    [HttpDelete("sessions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status401Unauthorized)]
    public Task<IActionResult> RevokeOtherSessions(
        [FromCurrentUser] Guid userId,
        [FromCurrentRefreshSession] Guid currentSessionId) =>
        HandleNoContent(userId.ToRevokeOtherSessionsCommand(currentSessionId));

    [AllowAnonymous]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EnableRateLimiting(PresentationPolicyNames.AuthRateLimitPolicyName)]
    public Task<IActionResult> Logout([FromBody] RefreshTokenHttpRequest? request = null) =>
        HandleLogoutAsync(request);

    private async Task<IActionResult> HandleLogoutAsync(RefreshTokenHttpRequest? request) {
        RefreshTokenCookieService refreshTokenCookies = HttpContext.RequestServices.GetRequiredService<RefreshTokenCookieService>();
        string? refreshToken = request?.RefreshToken ?? refreshTokenCookies.Read(HttpContext);
        try {
            return await HandleNoContent(refreshToken.ToLogoutCommand()).ConfigureAwait(false);
        } finally {
            refreshTokenCookies.Delete(HttpContext);
        }
    }
}
