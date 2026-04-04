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
public class DietologistController(ISender mediator) : AuthorizedController(mediator) {
    [HttpPost("invite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Invite([FromCurrentUser] Guid userId, [FromBody] InviteDietologistHttpRequest request) =>
        HandleNoContent(request.ToCommand(userId));

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

    [HttpDelete("relationship")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> RevokeOrDisconnect([FromCurrentUser] Guid userId) =>
        HandleNoContent(new Application.Dietologist.Commands.RevokeInvitation.RevokeInvitationCommand(userId));

    [HttpPut("permissions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> UpdatePermissions([FromCurrentUser] Guid userId, [FromBody] UpdateDietologistPermissionsHttpRequest request) =>
        HandleNoContent(request.ToCommand(userId));

    [HttpGet("my-dietologist")]
    [ProducesResponseType<DietologistInfoHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetMyDietologist([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToMyDietologistQuery(), static value => value?.ToHttpResponse());

    [HttpGet("invitation/{invitationId:guid}")]
    [ProducesResponseType<InvitationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetInvitation(Guid invitationId) =>
        HandleOk(invitationId.ToInvitationQuery(), static value => value.ToHttpResponse());
}
