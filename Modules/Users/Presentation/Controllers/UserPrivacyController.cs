using FoodDiary.Mediator;
using FoodDiary.Modules.Users.Presentation.Mappings;
using FoodDiary.Modules.Users.Presentation.Requests;
using FoodDiary.Modules.Users.Presentation.Contracts.Responses;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Presentation.Api.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Users.Presentation.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/users/privacy")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class UserPrivacyController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    [ProducesResponseType<UserPrivacyHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetPrivacy([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToPrivacyQuery(), static value => value.ToHttpResponse());

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status403Forbidden)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    [BlockImpersonatedAccess]
    public Task<IActionResult> UpdatePrivacy([FromCurrentUser] Guid userId, [FromBody] UpdatePrivacyConsentHttpRequest request) =>
        HandleNoContent(request.ToPrivacyCommand(userId));
}
