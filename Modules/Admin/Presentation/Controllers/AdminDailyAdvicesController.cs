using FoodDiary.Mediator;
using FoodDiary.Modules.Admin.Application.Commands.ImportAdminDailyAdvices;
using FoodDiary.Modules.Admin.Application.Queries.GetAdminDailyAdvices;
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
        HandleOk(new GetAdminDailyAdvicesQuery(), static items => items.Select(item =>
            new AdminDailyAdviceHttpResponse(item.Id, item.Locale, item.Value, item.Tag, item.Weight)).ToList());

    [HttpPost("import")]
    [EnableIdempotency(requireKey: true)]
    [RequestSizeLimit(PresentationRequestLimits.AdminImportPayloadBytes)]
    [RejectOversizedRequest(PresentationRequestLimits.AdminImportPayloadBytes)]
    [ProducesResponseType<AdminDailyAdvicesImportHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    [ProducesApiErrorResponse(StatusCodes.Status413PayloadTooLarge)]
    public Task<IActionResult> Import([FromBody] AdminDailyAdvicesImportHttpRequest request) =>
        HandleOk(new ImportAdminDailyAdvicesCommand(request.Version,
            request.Advices?.Select(item => item is null ? null! : new ImportAdminDailyAdviceItem(item.Value, item.Locale, item.Weight, item.Tag)).ToArray()!),
            static result => new AdminDailyAdvicesImportHttpResponse(result.ImportedCount, result.SkippedCount));
}
