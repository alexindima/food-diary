using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Filters;
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
[Route("api/v{version:apiVersion}/admin/achievement-definitions")]
[Authorize(Roles = PresentationRoleNames.Admin)]
public sealed class AdminAchievementDefinitionsController(ISender mediator) : BaseApiController(mediator) {
    [HttpGet]
    [ProducesResponseType<List<AdminAchievementDefinitionHttpResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetAll() =>
        HandleOk(AdminAchievementDefinitionsHttpMappings.ToQuery(),
            static definitions => definitions.Select(static definition => definition.ToHttpResponse()).ToList());

    [HttpPost]
    [EnableIdempotency]
    [ProducesResponseType<AdminAchievementDefinitionHttpResponse>(StatusCodes.Status201Created)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Create([FromBody] CreateAdminAchievementDefinitionHttpRequest request) =>
        HandleCreated(request.ToCommand(), static definition => definition.ToHttpResponse());

    [HttpPut("{id:guid}")]
    [ProducesResponseType<AdminAchievementDefinitionHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Update(Guid id, [FromBody] UpdateAdminAchievementDefinitionHttpRequest request) =>
        HandleOk(request.ToCommand(id), static definition => definition.ToHttpResponse());
}
