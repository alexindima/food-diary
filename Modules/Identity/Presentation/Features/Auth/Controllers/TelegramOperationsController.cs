using FoodDiary.Modules.Identity.Presentation.Security;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Mappings;
using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Responses;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FoodDiary.Modules.Identity.Presentation.Features.Auth.Controllers;

[ApiController]
[AllowAnonymous]
[RequireTelegramBotSecret]
[Route("api/v{version:apiVersion}/auth/telegram/bot/operations")]
[RequestSizeLimit(65536)]
[RejectOversizedRequest(65536)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[EnableRateLimiting(PresentationPolicyNames.AuthRateLimitPolicyName)]
[ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
[ProducesApiErrorResponse(StatusCodes.Status401Unauthorized)]
[ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
[ProducesApiErrorResponse(StatusCodes.Status413PayloadTooLarge)]
[ProducesApiErrorResponse(StatusCodes.Status429TooManyRequests)]
public sealed class TelegramOperationsController(ISender mediator) : BaseApiController(mediator) {
    [HttpPost]
    [ProducesResponseType<TelegramOperationRegisteredHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> Register([FromBody] RegisterTelegramOperationHttpRequest request) =>
        HandleOk(request.ToCommand(),
            static id => new TelegramOperationRegisteredHttpResponse(id));

    [HttpGet("ready")]
    [ProducesResponseType<IReadOnlyList<Guid>>(StatusCodes.Status200OK)]
    public Task<IActionResult> ListReady() => HandleOk(TelegramOperationHttpMappings.ToReadyQuery(), static ids => ids);

    [HttpPost("{operationId:guid}/lease")]
    [ProducesResponseType<TelegramOperationLeaseHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> Acquire(Guid operationId) => HandleOk(operationId.ToAcquireCommand(),
        static lease => new TelegramOperationLeaseHttpResponse(lease.OperationId, lease.LeaseId, lease.UserId, lease.SecurityVersion,
            lease.Payload, lease.Checkpoint, lease.LeaseExpiresAtUtc, lease.CreatedAtUtc));

    [HttpPost("{operationId:guid}/checkpoint")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> Checkpoint(Guid operationId, [FromBody] CheckpointTelegramOperationHttpRequest request) =>
        HandleNoContent(request.ToCommand(operationId));
}
