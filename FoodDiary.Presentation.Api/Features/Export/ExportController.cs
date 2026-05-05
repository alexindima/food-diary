using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Export.Mappings;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Export;

[ApiController]
[Route("api/v{version:apiVersion}/export")]
public class ExportController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet("diary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<IActionResult> ExportDiary(
        [FromCurrentUser] Guid userId,
        [FromQuery] DateTime dateFrom,
        [FromQuery] DateTime dateTo,
        [FromQuery] string format = "csv",
        [FromQuery] string? locale = null,
        [FromQuery] int? timeZoneOffsetMinutes = null,
        [FromQuery] string? reportOrigin = null) =>
        HandleFile(ExportHttpMappings.ToQuery(userId, dateFrom, dateTo, format, locale, timeZoneOffsetMinutes, reportOrigin));
}
