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
[Route("api/v{version:apiVersion}/admin/mail-inbox/messages")]
[Authorize(Roles = PresentationRoleNames.Admin)]
public sealed class AdminMailInboxController(ISender mediator) : BaseApiController(mediator) {
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AdminMailInboxMessageSummaryHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetMessages([FromQuery] GetAdminMailInboxMessagesHttpQuery query) =>
        HandleOk(query.ToQuery(), static value => value.Select(item => item.ToHttpResponse()).ToList());

    [HttpGet("{id:guid}")]
    [ProducesResponseType<AdminMailInboxMessageDetailsHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetMessage(Guid id) =>
        HandleOk(id.ToMailInboxMessageDetailsQuery(), static value => value.ToHttpResponse());
}
