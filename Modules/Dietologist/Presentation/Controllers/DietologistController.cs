using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Modules.Dietologist.Presentation.Mappings;
using FoodDiary.Modules.Dietologist.Presentation.Requests;
using FoodDiary.Modules.Dietologist.Presentation.Contracts.Responses;
using FoodDiary.Modules.Dietologist.Presentation.Responses;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Presentation.Api.Security;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Dietologist.Presentation.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/dietologist")]
public sealed class DietologistController(ISender mediator) : AuthorizedController(mediator) {
    [HttpPost("invite")]
    [EnableIdempotency]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    [BlockImpersonatedAccess]
    public Task<IActionResult> Invite([FromCurrentUser] Guid userId, [FromBody] InviteDietologistHttpRequest request) =>
        HandleNoContent(request.ToCommand(userId));

    [HttpDelete("relationship")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    [BlockImpersonatedAccess]
    public Task<IActionResult> RevokeOrDisconnect([FromCurrentUser] Guid userId) =>
        HandleNoContent(userId.ToRevokeInvitationCommand());

    [HttpPut("permissions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    [BlockImpersonatedAccess]
    public Task<IActionResult> UpdatePermissions([FromCurrentUser] Guid userId, [FromBody] UpdateDietologistPermissionsHttpRequest request) =>
        HandleNoContent(request.ToCommand(userId));

    [HttpGet("my-dietologist")]
    [ProducesResponseType<DietologistInfoHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> GetMyDietologist([FromCurrentUser] Guid userId) =>
        HandleOptional(userId.ToMyDietologistQuery(), static value => value?.ToHttpResponse());

    [HttpGet("relationship")]
    [ProducesResponseType<DietologistRelationshipHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> GetRelationship([FromCurrentUser] Guid userId) =>
        HandleOptional(userId.ToMyDietologistRelationshipQuery(), static value => value?.ToHttpResponse());
}
