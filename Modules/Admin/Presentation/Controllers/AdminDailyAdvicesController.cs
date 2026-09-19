using FoodDiary.Mediator;
using FoodDiary.Modules.Admin.Presentation.Mappings;
using FoodDiary.Modules.Admin.Presentation.Requests;
using FoodDiary.Modules.Admin.Presentation.Responses;
using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Admin.Presentation.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/admin/daily-advices")]
[Authorize(Roles = PresentationRoleNames.Admin)]
public sealed class AdminDailyAdvicesController(ISender mediator) : BaseApiController(mediator) {
    [HttpGet]
    [ProducesResponseType<List<AdminDailyAdviceHttpResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetAll() =>
        HandleOk(AdminHttpQueryMappings.ToDailyAdvicesQuery(), static items =>
            items.Select(static item => item.ToDailyAdviceHttpResponse()).ToList());

    [HttpPost("import")]
    [EnableIdempotency(requireKey: true)]
    [RequestSizeLimit(PresentationRequestLimits.AdminImportPayloadBytes)]
    [RejectOversizedRequest(PresentationRequestLimits.AdminImportPayloadBytes)]
    [ProducesResponseType<AdminDailyAdvicesImportHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    [ProducesApiErrorResponse(StatusCodes.Status413PayloadTooLarge)]
    public Task<IActionResult> Import([FromBody] AdminDailyAdvicesImportHttpRequest request) =>
        HandleOk(request.ToImportCommand(), static result => result.ToDailyAdvicesImportHttpResponse());

    [HttpGet("groups")]
    [ProducesResponseType<List<AdminDailyAdviceGroupHttpResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetGroups() =>
        HandleOk(AdminDailyAdviceGroupHttpMappings.ToDailyAdviceGroupsQuery(), static items =>
            items.Select(static item => item.ToGroupHttpResponse()).ToList());

    [HttpPost("groups/import")]
    [EnableIdempotency(requireKey: true)]
    [RequestSizeLimit(PresentationRequestLimits.AdminImportPayloadBytes)]
    [RejectOversizedRequest(PresentationRequestLimits.AdminImportPayloadBytes)]
    [ProducesResponseType<AdminDailyAdvicesImportHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    [ProducesApiErrorResponse(StatusCodes.Status413PayloadTooLarge)]
    public Task<IActionResult> ImportPairs([FromBody] AdminDailyAdvicePairsImportHttpRequest request) =>
        HandleOk(request.ToImportDailyAdvicePairsCommand(), static result => result.ToPairImportHttpResponse());

    [HttpPut("groups/{id:guid}")]
    [ProducesResponseType<AdminDailyAdviceGroupHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> UpdateGroup(Guid id, [FromBody] AdminDailyAdviceGroupUpdateHttpRequest request) =>
        HandleOk(request.ToUpdateDailyAdviceGroupCommand(id), static result => result.ToGroupHttpResponse());

    [HttpDelete("groups/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> DeleteGroup(Guid id) =>
        HandleNoContent(AdminDailyAdviceGroupHttpMappings.ToDeleteDailyAdviceGroupCommand(id));
}
