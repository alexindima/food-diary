using System;
using System.Threading.Tasks;
using FoodDiary.Application.Statistics.Queries.GetStatistics;
using FoodDiary.WebApi.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatisticsController(ISender mediator) : AuthorizedController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] DateTime dateFrom,
        [FromQuery] DateTime dateTo,
        [FromQuery] int quantizationDays = 1)
    {
        var query = new GetStatisticsQuery(CurrentUserId, dateFrom, dateTo, quantizationDays);
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }
}
