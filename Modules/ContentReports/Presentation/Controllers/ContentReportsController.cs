using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Modules.ContentReports.Presentation.Mappings;
using FoodDiary.Modules.ContentReports.Presentation.Requests;
using FoodDiary.Modules.ContentReports.Presentation.Responses;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.ContentReports.Presentation.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/reports")]
public sealed class ContentReportsController(ISender mediator) : AuthorizedController(mediator) {
    [HttpPost]
    [EnableIdempotency]
    [ProducesResponseType<ContentReportHttpResponse>(StatusCodes.Status201Created)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Create(
        [FromCurrentUser] Guid userId,
        [FromBody] CreateContentReportHttpRequest request) =>
        HandleCreated(request.ToCommand(userId), static value => value.ToHttpResponse());
}
