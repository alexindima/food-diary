using FoodDiary.Application.Admin.Queries.GetAdminDashboardSummary;
using FoodDiary.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FoodDiary.WebApi.Extensions;

namespace FoodDiary.WebApi.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class AdminDashboardController(ISender mediator) : BaseApiController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> GetDashboard([FromQuery] int recent = 5)
    {
        var query = new GetAdminDashboardSummaryQuery(Math.Clamp(recent, 1, 20));
        var result = await Mediator.Send(query);

        return result.ToActionResult();
    }
}
