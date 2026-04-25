using FoodDiary.MailInbox.Application.Messages.Queries;
using FoodDiary.MailInbox.Presentation.Controllers;
using FoodDiary.MailInbox.Presentation.Features.Messages.Mappings;
using FoodDiary.MailInbox.Presentation.Features.Messages.Responses;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.MailInbox.Presentation.Features.Messages;

[Route("api/mail-inbox/messages")]
public sealed class MailInboxMessagesController(ISender sender) : MailInboxControllerBase(sender) {
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<InboundMailMessageSummaryHttpResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> Get([FromQuery] int? limit) =>
        HandleOk(limit.ToQuery(), static value => value.ToHttpResponse());

    [HttpGet("{id:guid}")]
    [ProducesResponseType<InboundMailMessageDetailsHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetById(Guid id) =>
        HandleOk(new GetInboundMailMessageDetailsQuery(id), static value => value.ToHttpResponse());
}
