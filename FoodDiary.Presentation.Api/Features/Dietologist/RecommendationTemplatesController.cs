using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Dietologist.Mappings;
using FoodDiary.Presentation.Api.Features.Dietologist.Requests;
using FoodDiary.Presentation.Api.Features.Dietologist.Responses;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Dietologist;

[ApiController]
[Authorize(Roles = PresentationRoleNames.Dietologist)]
[Route("api/v{version:apiVersion}/dietologist/recommendation-templates")]
public sealed class RecommendationTemplatesController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    [ProducesResponseType<List<RecommendationTemplateHttpResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> Search(
        [FromCurrentUser] Guid userId,
        [FromQuery] string? search = null,
        [FromQuery] bool includeArchived = false) =>
        HandleOk(
            userId.ToSearchTemplatesQuery(search, includeArchived),
            static value => value.Select(template => template.ToHttpResponse()).ToList());

    [HttpPost]
    [ProducesResponseType<RecommendationTemplateHttpResponse>(StatusCodes.Status201Created)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Create(
        [FromCurrentUser] Guid userId,
        [FromBody] RecommendationTemplateHttpRequest request) =>
        HandleCreated(request.ToCreateTemplateCommand(userId), static value => value.ToHttpResponse());

    [HttpPut("{templateId:guid}")]
    [ProducesResponseType<RecommendationTemplateHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Update(
        Guid templateId,
        [FromCurrentUser] Guid userId,
        [FromBody] RecommendationTemplateHttpRequest request) =>
        HandleOk(request.ToUpdateTemplateCommand(templateId, userId), static value => value.ToHttpResponse());

    [HttpDelete("{templateId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Archive(Guid templateId, [FromCurrentUser] Guid userId) =>
        HandleNoContent(templateId.ToArchiveTemplateCommand(userId));
}
