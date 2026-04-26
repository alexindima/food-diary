using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Lessons.Mappings;
using FoodDiary.Presentation.Api.Features.Lessons.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Lessons;

[ApiController]
[Route("api/v{version:apiVersion}/lessons")]
public class LessonsController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<LessonSummaryHttpResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetAll(
        [FromCurrentUser] Guid userId,
        [FromQuery] string locale = "en",
        [FromQuery] string? category = null) =>
        HandleOk(userId.ToQuery(locale, category), static value => value.ToHttpResponse());

    [HttpGet("{id:guid}")]
    [ProducesResponseType<LessonDetailHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetById(
        [FromCurrentUser] Guid userId,
        Guid id) =>
        HandleOk(userId.ToGetByIdQuery(id), static value => value.ToHttpResponse());

    [HttpPost("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> MarkRead(
        [FromCurrentUser] Guid userId,
        Guid id) =>
        HandleNoContent(userId.ToMarkReadCommand(id));
}
