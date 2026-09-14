using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Modules.Admin.Presentation.Mappings;
using FoodDiary.Modules.Admin.Presentation.Requests;
using FoodDiary.Modules.Admin.Presentation.Responses;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Admin.Presentation.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/admin/bugs")]
[Authorize(Roles = PresentationRoleNames.Admin)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminBugReportsController(ISender mediator) : BaseApiController(mediator) {
    [HttpGet]
    [ProducesResponseType<AdminBugReportPageHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetPage([FromQuery] GetAdminBugReportsHttpQuery query) =>
        HandleOk(query.ToQuery(), static page => page.ToHttpResponse());
}
