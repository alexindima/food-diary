using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Modules.Admin.Presentation.Features.Admin.Mappings;
using FoodDiary.Modules.Admin.Presentation.Features.Admin.Requests;
using FoodDiary.Modules.Admin.Presentation.Features.Admin.Responses;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Admin.Presentation.Features.Admin;

[ApiController]
[Route("api/v{version:apiVersion}/admin/outgoing-emails")]
[Authorize(Roles = PresentationRoleNames.Admin)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminOutgoingEmailsController(ISender mediator) : BaseApiController(mediator) {
    [HttpGet]
    [ProducesResponseType<AdminOutgoingEmailPageHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetPage([FromQuery] GetAdminOutgoingEmailsHttpQuery query) =>
        HandleOk(query.ToQuery(), static value => value.ToHttpResponse());
}
