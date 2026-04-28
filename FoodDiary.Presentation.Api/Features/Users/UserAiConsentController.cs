using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Users.Mappings;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Presentation.Api.Security;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Users;

[ApiController]
[Route("api/v{version:apiVersion}/users")]
public class UserAiConsentController(ISender mediator) : AuthorizedController(mediator) {
    [HttpPost("ai-consent")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    [BlockImpersonatedAccess]
    public Task<IActionResult> AcceptAiConsent([FromCurrentUser] Guid userId) =>
        HandleNoContent(userId.ToAcceptAiConsentCommand());

    [HttpDelete("ai-consent")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    [BlockImpersonatedAccess]
    public Task<IActionResult> RevokeAiConsent([FromCurrentUser] Guid userId) =>
        HandleNoContent(userId.ToRevokeAiConsentCommand());
}
