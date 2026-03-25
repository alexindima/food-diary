using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Admin.Mappings;
using FoodDiary.Presentation.Api.Features.Admin.Requests;
using FoodDiary.Presentation.Api.Features.Admin.Responses;
using FoodDiary.Presentation.Api.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Admin;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = PresentationRoleNames.Admin)]
public sealed class AdminUsersController(ISender mediator) : BaseApiController(mediator) {
    [HttpGet]
    [ProducesResponseType<PagedHttpResponse<AdminUserHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetUsers([FromQuery] GetAdminUsersHttpQuery query) =>
        HandleOk(query.ToQuery(), static value => value.ToHttpResponse());

    [HttpPatch("{id:guid}")]
    [ProducesResponseType<AdminUserHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> UpdateUser(Guid id, [FromBody] AdminUserUpdateHttpRequest request) =>
        HandleOk(request.ToCommand(id), static value => value.ToHttpResponse());
}

