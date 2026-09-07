using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Admin.Mappings;
using FoodDiary.Presentation.Api.Features.Admin.Requests;
using FoodDiary.Presentation.Api.Features.Admin.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Admin;

[ApiController]
[Route("api/v{version:apiVersion}/admin/dashboard")]
[Authorize(Roles = PresentationRoleNames.Admin)]
public sealed class AdminDashboardController(ISender mediator) : BaseApiController(mediator) {
    [HttpGet("overview")]
    [ProducesResponseType<AdminDashboardOverviewHttpResponse>(StatusCodes.Status200OK)]
    [FoodDiary.Presentation.Api.Responses.ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public Task<IActionResult> GetOverview([FromQuery] GetAdminDashboardOverviewHttpQuery query) =>
        HandleOk(query.ToQuery(), static value => value.ToHttpResponse());

    [HttpGet]
    [ProducesResponseType<AdminDashboardSummaryHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetDashboard([FromQuery] GetAdminDashboardHttpQuery query) =>
        HandleOk(query.ToQuery(), static value => value.ToHttpResponse());
}
