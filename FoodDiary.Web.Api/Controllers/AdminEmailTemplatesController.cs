using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FoodDiary.Application.Admin.Commands.UpsertAdminEmailTemplate;
using FoodDiary.Application.Admin.Queries.GetAdminEmailTemplates;
using FoodDiary.Contracts.Admin;
using FoodDiary.Domain.Enums;
using FoodDiary.WebApi.Extensions;

namespace FoodDiary.WebApi.Controllers;

[ApiController]
[Route("api/admin/email-templates")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class AdminEmailTemplatesController(ISender mediator) : BaseApiController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await Mediator.Send(new GetAdminEmailTemplatesQuery());
        return result.ToActionResult();
    }

    [HttpPut("{key}/{locale}")]
    public async Task<IActionResult> Upsert(
        string key,
        string locale,
        [FromBody] AdminEmailTemplateUpsertRequest request)
    {
        var command = new UpsertAdminEmailTemplateCommand(
            key,
            locale,
            request.Subject,
            request.HtmlBody,
            request.TextBody,
            request.IsActive);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }
}
