using FoodDiary.Application.Admin.Queries.GetAdminDashboardSummary;
using FoodDiary.Domain.Enums;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Admin;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class AdminDashboardController(ISender mediator) : BaseApiController(mediator) {
    [HttpGet]
    public async Task<IActionResult> GetDashboard([FromQuery] int recent = 5) {
        var query = new GetAdminDashboardSummaryQuery(Math.Clamp(recent, 1, 20));
        var result = await Mediator.Send(query);

        return result.ToActionResult();
    }
}
