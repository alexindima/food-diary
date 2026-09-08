using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Presentation.Api.Features.Admin.Mappings;
using FoodDiary.Presentation.Api.Features.Admin.Requests;
using FoodDiary.Presentation.Api.Features.Admin.Responses;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Admin;

[ApiController]
[Route("api/v{version:apiVersion}/admin/email-templates")]
[Authorize(Roles = PresentationRoleNames.Admin)]
public sealed class AdminEmailTemplatesController(ISender mediator) : BaseApiController(mediator) {
    [HttpGet]
    [ProducesResponseType<List<AdminEmailTemplateHttpResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetAll() =>
        HandleOk(AdminHttpQueryMappings.ToEmailTemplatesQuery(), static value => value.Select(item => item.ToHttpResponse()).ToList());

    [HttpGet("{key:maxlength(64)}/{locale:maxlength(10)}/revisions")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType<List<AdminTemplateRevisionHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetRevisions(string key, string locale) =>
        HandleOk(AdminTemplateRevisionHttpMappings.ToTemplateRevisionsQuery(key, locale, isAiPrompt: false),
            static value => value.Select(item => item.ToRevisionHttpResponse()).ToList());

    [HttpPut("{key:maxlength(64)}/{locale:maxlength(10)}")]
    [ProducesResponseType<AdminEmailTemplateHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Upsert(
        string key,
        string locale,
        [FromBody] AdminEmailTemplateUpsertHttpRequest request) =>
        HandleOk(request.ToCommand(key, locale), static value => value.ToHttpResponse());

    [HttpPost("test")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status429TooManyRequests)]
    [EnableRateLimiting(PresentationPolicyNames.TestDeliveryRateLimitPolicyName)]
    [EnableIdempotency]
    public Task<IActionResult> SendTest([FromBody] AdminEmailTemplateTestHttpRequest request) =>
        HandleNoContent(request.ToCommand());
}
