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
[Route("api/v{version:apiVersion}/admin/ai-prompts")]
[Authorize(Roles = PresentationRoleNames.Admin)]
public sealed class AdminAiPromptsController(ISender mediator) : BaseApiController(mediator) {
    [HttpGet]
    [ProducesResponseType<List<AdminAiPromptHttpResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetAll() =>
        HandleOk(AdminHttpQueryMappings.ToAiPromptsQuery(), static value => value.Select(item => item.ToAiPromptHttpResponse()).ToList());

    [HttpGet("{key:maxlength(64)}/{locale:maxlength(10)}/revisions")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType<List<AdminTemplateRevisionHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetRevisions(string key, string locale) =>
        HandleOk(AdminTemplateRevisionHttpMappings.ToTemplateRevisionsQuery(key, locale, isAiPrompt: true),
            static value => value.Select(item => item.ToRevisionHttpResponse()).ToList());

    [HttpPut("{key:maxlength(64)}/{locale:maxlength(10)}")]
    [ProducesResponseType<AdminAiPromptHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Upsert(
        string key,
        string locale,
        [FromBody] AdminAiPromptUpsertHttpRequest request) =>
        HandleOk(request.ToCommand(key, locale), static value => value.ToAiPromptHttpResponse());
}
