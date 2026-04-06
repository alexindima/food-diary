using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.ContentReports.Mappings;
using FoodDiary.Presentation.Api.Features.ContentReports.Requests;
using FoodDiary.Presentation.Api.Features.ContentReports.Responses;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.ContentReports;

[ApiController]
[Route("api/v{version:apiVersion}/reports")]
public class ContentReportsController(ISender mediator) : AuthorizedController(mediator) {
    [HttpPost]
    [ProducesResponseType<ContentReportHttpResponse>(StatusCodes.Status201Created)]
    public Task<IActionResult> Create(
        [FromCurrentUser] Guid userId,
        [FromBody] CreateContentReportHttpRequest request) =>
        HandleCreated(request.ToCommand(userId), static value => value.ToHttpResponse());
}
