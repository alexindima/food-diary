using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Auth.Mappings;
using FoodDiary.Presentation.Api.Features.Auth.Requests;
using FoodDiary.Presentation.Api.Features.Auth.Responses;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Presentation.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FoodDiary.Presentation.Api.Features.Auth;

[ApiController]
[Route("api/v{version:apiVersion}/auth/telegram")]
[RequestSizeLimit(AuthRequestLimits.MaxPayloadBytes)]
[RejectOversizedRequest(AuthRequestLimits.MaxPayloadBytes)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[EnableRateLimiting(PresentationPolicyNames.AuthRateLimitPolicyName)]
[ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
[ProducesApiErrorResponse(StatusCodes.Status401Unauthorized)]
[ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
[ProducesApiErrorResponse(StatusCodes.Status413PayloadTooLarge)]
[ProducesApiErrorResponse(StatusCodes.Status429TooManyRequests)]
public sealed class AuthTelegramOnboardingController(ISender mediator, TelegramBrowserBindingHttpProcessor bindings) : BaseApiController(mediator) {
    [AllowAnonymous]
    [HttpGet("configuration")]
    [ProducesResponseType<TelegramConfigurationHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> Configuration() => HandleOk(TelegramOnboardingHttpMappings.ToConfigurationQuery(),
        static model => new TelegramConfigurationHttpResponse(model.LoginEnabled, model.RegistrationEnabled, model.OidcEnabled));
    [AllowAnonymous]
    [HttpPost("oidc/start")]
    [ProducesResponseType<TelegramOidcStartHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> StartOidc() =>
        HandleOk(bindings.Ensure(HttpContext).ToStartOidcCommand(), static model => model.ToHttpResponse());

    [Authorize]
    [BlockImpersonatedAccess]
    [HttpPost("oidc/start-link")]
    [ProducesResponseType<TelegramOidcStartHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> StartOidcLink([FromCurrentUser] Guid userId) =>
        HandleOk(bindings.Ensure(HttpContext).ToStartOidcCommand(userId), static model => model.ToHttpResponse());

    [AllowAnonymous]
    [HttpPost("oidc/exchange")]
    [ProducesResponseType<TelegramAuthenticationIntentHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> ExchangeOidc([FromBody] ExchangeTelegramOidcHttpRequest request) =>
        HandleOk(request.ToExchangeCommand(bindings.Read(HttpContext)), static model => model.ToHttpResponse());

    [AllowAnonymous]
    [HttpPost("mini-app/begin")]
    [ProducesResponseType<TelegramAuthenticationIntentHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> Begin([FromBody] TelegramAuthHttpRequest request) =>
        HandleOk(request.ToBeginCommand(bindings.Ensure(HttpContext)), static model => model.ToHttpResponse());

    [Authorize]
    [BlockImpersonatedAccess]
    [HttpPost("mini-app/begin-link")]
    [ProducesResponseType<TelegramAuthenticationIntentHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> BeginLink([FromCurrentUser] Guid userId, [FromBody] TelegramAuthHttpRequest request) =>
        HandleOk(request.ToBeginCommand(bindings.Ensure(HttpContext), userId), static model => model.ToHttpResponse());

    [AllowAnonymous]
    [HttpPost("complete")]
    [ProducesResponseType<AuthenticationHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> Complete([FromBody] CompleteTelegramAuthenticationHttpRequest request) =>
        HandleOk(request.ToCompleteCommand(bindings.Read(HttpContext), HttpContext), static model => model.ToHttpResponse());

    [Authorize]
    [BlockImpersonatedAccess]
    [HttpPost("complete-link")]
    [ProducesResponseType<AuthenticationHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> CompleteLink([FromCurrentUser] Guid userId, [FromBody] CompleteTelegramLinkHttpRequest request) =>
        HandleOk(request.ToCompleteLinkCommand(bindings.Read(HttpContext), userId, HttpContext), static model => model.ToHttpResponse());
}
