using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Admin.Mappings;
using FoodDiary.Presentation.Api.Features.Admin.Requests;
using FoodDiary.Presentation.Api.Features.Admin.Responses;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Admin;

[ApiController]
[Route("api/v{version:apiVersion}/admin/users/collaboration-audit")]
[Authorize(Roles = PresentationRoleNames.Admin)]
public sealed class AdminCollaborationAuditController(ISender mediator) : BaseApiController(mediator) {
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AdminAuditEntryHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetCollaborationAudit([FromQuery] GetCollaborationAuditHttpQuery query) =>
        HandleOk(query.ToQuery(), static value => value.Select(item => item.ToHttpResponse()).ToList());
}
