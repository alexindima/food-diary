using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Presentation.Api.Policies;
using Microsoft.AspNetCore.RateLimiting;
using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Modules.Admin.Presentation.Mappings;
using FoodDiary.Modules.Admin.Presentation.Requests;
using FoodDiary.Modules.Admin.Presentation.Responses;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Admin.Presentation.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/admin/ai-prompts")]
[Authorize(Roles = PresentationRoleNames.Admin)]
public sealed class AdminAiPromptsController(ISender mediator) : BaseApiController(mediator) {
    [HttpGet("scenarios")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType<List<AdminAiPromptScenarioHttpResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetScenarios() => HandleOk(AdminAiPromptWorkbenchHttpMappings.ToScenariosQuery(),
        static value => value.Select(item => item.ToScenarioHttpResponse()).ToList());

    [HttpPost("preview")]
    [RequestSizeLimit(PresentationRequestLimits.AiPayloadBytes)]
    [RejectOversizedRequest(PresentationRequestLimits.AiPayloadBytes)]
    [ProducesResponseType<AdminAiPromptInspectionHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Preview([FromBody] AdminAiPromptDraftHttpRequest request) =>
        HandleOk(request.ToPreviewQuery(), static text => new AdminAiPromptInspectionHttpResponse(text));

    [HttpPost("test")]
    [EnableIdempotency(requireKey: true)]
    [EnableRateLimiting(PresentationPolicyNames.AiRateLimitPolicyName)]
    [RequestSizeLimit(PresentationRequestLimits.AiPayloadBytes)]
    [RejectOversizedRequest(PresentationRequestLimits.AiPayloadBytes)]
    [ProducesResponseType<AdminAiPromptInspectionHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status429TooManyRequests)]
    [ProducesApiErrorResponse(StatusCodes.Status502BadGateway)]
    public Task<IActionResult> Test([FromCurrentUser] Guid userId, [FromBody] AdminAiPromptDraftHttpRequest request) =>
        HandleOk(request.ToTestCommand(userId,
            IdempotencyRequestContext.GetRequestId(HttpContext) ?? throw new InvalidOperationException("Required idempotency context is unavailable.")), static text => new AdminAiPromptInspectionHttpResponse(text));

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
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Upsert(
        string key,
        string locale,
        [FromBody] AdminAiPromptUpsertHttpRequest request) =>
        HandleOk(request.ToCommand(key, locale), static value => value.ToAiPromptHttpResponse());
}
