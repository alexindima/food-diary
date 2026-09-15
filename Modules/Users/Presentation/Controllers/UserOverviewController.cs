using FoodDiary.Modules.Users.Presentation.Mappings.Mappings;
using FoodDiary.Modules.Users.Presentation.Mappings;
using FoodDiary.Presentation.Api.Controllers;

using FoodDiary.Modules.Users.Presentation.Responses;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Users.Presentation.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/users")]
public sealed class UserOverviewController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet("overview")]
    [ProducesResponseType<ProfileOverviewHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetProfileOverview([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToProfileOverviewQuery(), static value => value.ToHttpResponse());
}
