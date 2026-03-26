using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Auth.Mappings;
using FoodDiary.Presentation.Api.Features.Auth.Requests;
using FoodDiary.Presentation.Api.Features.Auth.Responses;
using FoodDiary.Presentation.Api.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Presentation.Api.Features.Auth;

[ApiController]
[Route("api/auth/admin-sso")]
public sealed class AdminSsoController(ISender mediator, ILogger<AdminSsoController> logger) : BaseApiController(mediator) {
    private readonly ILogger<AdminSsoController> _logger = logger;

    [Authorize(Roles = PresentationRoleNames.Admin)]
    [HttpPost("start")]
    [ProducesResponseType<AdminSsoStartHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> AdminSsoStart([FromCurrentUser] Guid userId) =>
        HandleObservedOk(userId.ToAdminSsoStartCommand(), static value => value.ToHttpResponse(), _logger, "auth.admin-sso.start", userId);

    [AllowAnonymous]
    [HttpPost("exchange")]
    [ProducesResponseType<AuthenticationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status401Unauthorized)]
    public Task<IActionResult> AdminSsoExchange([FromBody] AdminSsoExchangeHttpRequest request) =>
        HandleObservedOk(request.ToCommand(), static value => value.ToHttpResponse(), _logger, "auth.admin-sso.exchange");
}
