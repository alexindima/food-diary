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
[Route("api/admin/email-templates")]
[Authorize(Roles = PresentationRoleNames.Admin)]
public sealed class AdminEmailTemplatesController(ISender mediator) : BaseApiController(mediator) {
    [HttpGet]
    public async Task<IActionResult> GetAll() {
        var result = await Mediator.Send(AdminHttpQueryMappings.ToEmailTemplatesQuery());
        return result.ToOkActionResult(this, static value => value.Select(item => item.ToHttpResponse()).ToList());
    }

    [HttpPut("{key}/{locale}")]
    public async Task<IActionResult> Upsert(
        string key,
        string locale,
        [FromBody] AdminEmailTemplateUpsertHttpRequest request) {
        var command = request.ToCommand(key, locale);
        var result = await Mediator.Send(command);
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }
}
