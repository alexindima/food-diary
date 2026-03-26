using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Auth.Mappings;
using FoodDiary.Presentation.Api.Features.Auth.Requests;
using FoodDiary.Presentation.Api.Features.Auth.Responses;
using FoodDiary.Presentation.Api.Responses;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Presentation.Api.Features.Auth;

[ApiController]
[Route("api/auth/password-reset")]
public sealed class AuthPasswordController(ISender mediator, ILogger<AuthPasswordController> logger) : BaseApiController(mediator) {
    private readonly ILogger<AuthPasswordController> _logger = logger;

    [HttpPost("request")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> RequestPasswordReset([FromBody] RequestPasswordResetHttpRequest request) =>
        HandleObservedNoContent(request.ToCommand(), _logger, "auth.password-reset.request");

    [HttpPost("confirm")]
    [ProducesResponseType<AuthenticationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status401Unauthorized)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> ConfirmPasswordReset([FromBody] ConfirmPasswordResetHttpRequest request) =>
        HandleObservedOk(request.ToCommand(), static value => value.ToHttpResponse(), _logger, "auth.password-reset.confirm");
}
