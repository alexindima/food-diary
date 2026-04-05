using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Exercises.Mappings;
using FoodDiary.Presentation.Api.Features.Exercises.Requests;
using FoodDiary.Presentation.Api.Features.Exercises.Responses;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Exercises;

[ApiController]
[Route("api/v{version:apiVersion}/exercises")]
public class ExercisesController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ExerciseEntryHttpResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetAll(
        [FromCurrentUser] Guid userId,
        [FromQuery] DateTime dateFrom,
        [FromQuery] DateTime dateTo) =>
        HandleOk(userId.ToQuery(dateFrom, dateTo), static value => value.ToHttpResponse());

    [HttpPost]
    [ProducesResponseType<ExerciseEntryHttpResponse>(StatusCodes.Status201Created)]
    public Task<IActionResult> Create(
        [FromCurrentUser] Guid userId,
        [FromBody] CreateExerciseEntryHttpRequest request) =>
        HandleCreated(request.ToCommand(userId), static value => value.ToHttpResponse());

    [HttpPut("{id:guid}")]
    [ProducesResponseType<ExerciseEntryHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> Update(
        [FromCurrentUser] Guid userId,
        Guid id,
        [FromBody] UpdateExerciseEntryHttpRequest request) =>
        HandleOk(request.ToCommand(userId, id), static value => value.ToHttpResponse());

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> Delete(
        [FromCurrentUser] Guid userId,
        Guid id) =>
        HandleNoContent(userId.ToDeleteCommand(id));
}
