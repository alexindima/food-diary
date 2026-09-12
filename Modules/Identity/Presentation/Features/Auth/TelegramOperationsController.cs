using FoodDiary.Application.Identity.Authentication.Commands.AcquireTelegramOperation;
using FoodDiary.Application.Identity.Authentication.Commands.CheckpointTelegramOperation;
using FoodDiary.Application.Identity.Authentication.Commands.RegisterTelegramOperation;
using FoodDiary.Application.Identity.Authentication.Queries.ListReadyTelegramOperations;
using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Controllers;
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
        HandleOk(new RegisterTelegramOperationCommand(request.UpdateId, request.TelegramUserId, request.Payload),
            static id => new TelegramOperationRegisteredHttpResponse(id));

    [HttpGet("ready")]
    [ProducesResponseType<IReadOnlyList<Guid>>(StatusCodes.Status200OK)]
    public Task<IActionResult> ListReady() => HandleOk(new ListReadyTelegramOperationsQuery(), static ids => ids);

    [HttpPost("{operationId:guid}/lease")]
    [ProducesResponseType<TelegramOperationLeaseHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> Acquire(Guid operationId) => HandleOk(new AcquireTelegramOperationCommand(operationId),
        static lease => new TelegramOperationLeaseHttpResponse(lease.OperationId, lease.LeaseId, lease.UserId, lease.SecurityVersion,
            lease.Payload, lease.Checkpoint, lease.LeaseExpiresAtUtc, lease.CreatedAtUtc));

    [HttpPost("{operationId:guid}/checkpoint")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> Checkpoint(Guid operationId, [FromBody] CheckpointTelegramOperationHttpRequest request) =>
        HandleNoContent(new CheckpointTelegramOperationCommand(operationId, request.LeaseId, request.Checkpoint,
            request.Completed, request.NextAttemptAtUtc));
}
