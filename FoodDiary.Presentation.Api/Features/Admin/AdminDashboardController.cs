using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Features.Admin.Mappings;
using FoodDiary.Presentation.Api.Features.Admin.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Admin;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Roles = PresentationRoleNames.Admin)]
public sealed class AdminDashboardController(ISender mediator) : BaseApiController(mediator) {
    [HttpGet]
    public async Task<IActionResult> GetDashboard([FromQuery] GetAdminDashboardHttpQuery query) {
        var result = await Mediator.Send(query.ToQuery());
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }
}
