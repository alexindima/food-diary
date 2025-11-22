using System;
using System.Threading.Tasks;
using FoodDiary.Application.Dashboard.Queries.GetDashboardSnapshot;
using FoodDiary.WebApi.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.WebApi.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController(ISender mediator) : AuthorizedController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] DateTime date,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetDashboardSnapshotQuery(CurrentUserId, date, page, pageSize);
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }
}
