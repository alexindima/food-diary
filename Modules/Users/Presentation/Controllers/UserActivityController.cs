using FoodDiary.Mediator;
using FoodDiary.Modules.Users.Presentation.Mappings;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Presentation.Api.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Users.Presentation.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/users/activity")]
[BlockImpersonatedAccess]
public sealed class UserActivityController(ISender mediator) : AuthorizedController(mediator) {
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> RecordActivity([FromCurrentUser] Guid userId) =>
        HandleNoContent(userId.ToActivityCommand());
}
