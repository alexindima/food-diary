using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Modules.Admin.Presentation.Mappings;
using FoodDiary.Modules.Admin.Presentation.Requests;
using FoodDiary.Modules.Admin.Presentation.Responses;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Admin.Presentation.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/admin/audit")]
[Authorize(Roles = PresentationRoleNames.Admin)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminAuditController(ISender mediator) : BaseApiController(mediator) {
    [HttpGet]
    [ProducesResponseType<AdminAuditPageHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetPage([FromQuery] GetAdminAuditHttpQuery query) =>
        HandleOk(query.ToQuery(), static page => new AdminAuditPageHttpResponse(page.Items.Select(item => item.ToHttpResponse()).ToList(), page.TotalItems));
}
