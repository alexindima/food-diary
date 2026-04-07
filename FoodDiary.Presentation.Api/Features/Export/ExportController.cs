using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Export.Mappings;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Export;

[ApiController]
[Route("api/v{version:apiVersion}/export")]
public class ExportController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet("diary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Produces("text/csv")]
    public Task<IActionResult> ExportDiary(
        [FromCurrentUser] Guid userId,
        [FromQuery] DateTime dateFrom,
        [FromQuery] DateTime dateTo) =>
        HandleFile(ExportHttpMappings.ToQuery(userId, dateFrom, dateTo));
}
