using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Admin.Mappings;
using FoodDiary.Presentation.Api.Features.Admin.Requests;
using FoodDiary.Presentation.Api.Features.Admin.Responses;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Admin;

[ApiController]
[Route("api/v{version:apiVersion}/admin/lessons")]
[Authorize(Roles = PresentationRoleNames.Admin)]
public sealed class AdminLessonsController(ISender mediator) : BaseApiController(mediator) {
    [HttpGet]
    [ProducesResponseType<List<AdminLessonHttpResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetAll() =>
        HandleOk(AdminHttpQueryMappings.ToLessonsQuery(), static value =>
            value.Select(static item => item.ToLessonHttpResponse()).ToList());

    [HttpPost]
    [ProducesResponseType<AdminLessonHttpResponse>(StatusCodes.Status201Created)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Create([FromBody] AdminLessonCreateHttpRequest request) =>
        HandleCreated(request.ToCreateCommand(), static value => value.ToLessonHttpResponse());

    [HttpPost("import")]
    [ProducesResponseType<AdminLessonsImportHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Import([FromBody] AdminLessonsImportHttpRequest request) =>
        HandleOk(request.ToImportCommand(), static value => value.ToLessonsImportHttpResponse());

    [HttpPut("{id:guid}")]
    [ProducesResponseType<AdminLessonHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Update(Guid id, [FromBody] AdminLessonUpdateHttpRequest request) =>
        HandleOk(request.ToUpdateCommand(id), static value => value.ToLessonHttpResponse());

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Delete(Guid id) =>
        HandleNoContent(id.ToDeleteCommand());
}
