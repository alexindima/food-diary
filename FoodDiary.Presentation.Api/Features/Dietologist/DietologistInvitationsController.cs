using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Dietologist.Mappings;
using FoodDiary.Presentation.Api.Features.Dietologist.Requests;
using FoodDiary.Presentation.Api.Features.Dietologist.Responses;
using FoodDiary.Presentation.Api.Responses;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Dietologist;

[ApiController]
[Route("api/v{version:apiVersion}/dietologist")]
public class DietologistInvitationsController(ISender mediator) : AuthorizedController(mediator) {
    [HttpPost("accept")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Accept([FromCurrentUser] Guid userId, [FromBody] AcceptInvitationHttpRequest request) =>
        HandleNoContent(request.ToCommand(userId));

    [HttpPost("decline")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Decline([FromCurrentUser] Guid userId, [FromBody] DeclineInvitationHttpRequest request) =>
        HandleNoContent(request.ToCommand(userId));

    [HttpGet("invitations/{invitationId:guid}/current-user")]
    [ProducesResponseType<DietologistInvitationForCurrentUserHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetInvitationForCurrentUser(Guid invitationId, [FromCurrentUser] Guid userId) =>
        HandleOk(invitationId.ToCurrentUserInvitationQuery(userId), static value => value.ToHttpResponse());

    [HttpPost("invitations/{invitationId:guid}/accept-current-user")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> AcceptForCurrentUser(Guid invitationId, [FromCurrentUser] Guid userId) =>
        HandleNoContent(invitationId.ToCurrentUserAcceptCommand(userId));

    [HttpPost("invitations/{invitationId:guid}/decline-current-user")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> DeclineForCurrentUser(Guid invitationId, [FromCurrentUser] Guid userId) =>
        HandleNoContent(invitationId.ToCurrentUserDeclineCommand(userId));

    [HttpGet("invitation/{invitationId:guid}")]
    [ProducesResponseType<InvitationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetInvitation(Guid invitationId) =>
        HandleOk(invitationId.ToInvitationQuery(), static value => value.ToHttpResponse());
}
