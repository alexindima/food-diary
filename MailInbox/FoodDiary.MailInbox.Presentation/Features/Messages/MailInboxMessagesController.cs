using FoodDiary.MailInbox.Application.Messages.Commands.MarkInboundMailMessageRead;
using FoodDiary.MailInbox.Application.Messages.Queries.GetInboundMailMessageDetails;
using FoodDiary.MailInbox.Presentation.Controllers;
using FoodDiary.MailInbox.Presentation.Features.Messages.Mappings;
using FoodDiary.MailInbox.Presentation.Features.Messages.Responses;
using FoodDiary.MailInbox.Presentation.Filters;
using FoodDiary.MailInbox.Presentation.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.MailInbox.Presentation.Features.Messages;

[Route("api/mail-inbox/messages")]
public sealed class MailInboxMessagesController(ISender sender) : AuthorizedMailInboxEndpointBase(sender) {
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<InboundMailMessageSummaryHttpResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> Get([FromQuery] int? limit) =>
        HandleOk(limit.ToQuery(), static value => value.ToHttpResponse());

    [HttpGet("{id:guid}")]
    [ServiceFilter(typeof(MailInboxMessageDetailConcurrencyFilter))]
    [ProducesResponseType<InboundMailMessageDetailsHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<MailInboxApiErrorHttpResponse>(StatusCodes.Status429TooManyRequests)]
    public Task<IActionResult> GetById(Guid id) =>
        HandleOk(new GetInboundMailMessageDetailsQuery(id), static value => value.ToHttpResponse());

    [HttpPost("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> MarkRead(Guid id) =>
        HandleNoContent(new MarkInboundMailMessageReadCommand(id));
}
