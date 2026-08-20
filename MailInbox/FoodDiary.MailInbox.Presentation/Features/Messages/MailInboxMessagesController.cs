using FoodDiary.MailInbox.Application.Messages.Commands.MarkInboundMailMessageRead;
using FoodDiary.MailInbox.Application.Messages.Queries.GetInboundMailMessageDetails;
using FoodDiary.MailInbox.Presentation.Controllers;
using FoodDiary.MailInbox.Presentation.Features.Messages.Mappings;
using FoodDiary.MailInbox.Presentation.Features.Messages.Responses;
using FoodDiary.MailInbox.Presentation.Filters;
using FoodDiary.MailInbox.Presentation.Options;
using FoodDiary.MailInbox.Presentation.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FoodDiary.MailInbox.Presentation.Features.Messages;

[Route("api/mail-inbox/messages")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class MailInboxMessagesController(
    ISender sender,
    IOptions<MailInboxHttpOptions> options) : AuthorizedMailInboxEndpointBase(sender) {
    [HttpGet]
    [ServiceFilter(typeof(MailInboxMessageMetadataConcurrencyFilter))]
    [ProducesResponseType<IReadOnlyList<InboundMailMessageSummaryHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<MailInboxApiErrorHttpResponse>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<MailInboxApiErrorHttpResponse>(StatusCodes.Status503ServiceUnavailable)]
    public Task<IActionResult> Get([FromQuery] int? limit) =>
        ExecuteMetadataOperationAsync(
            cancellationToken => HandleOk(
                limit.ToQuery(),
                static value => value.ToHttpResponse(),
                cancellationToken));

    [HttpGet("{id:guid}")]
    [ServiceFilter(typeof(MailInboxMessageDetailConcurrencyFilter))]
    [ProducesResponseType<InboundMailMessageDetailsHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<MailInboxApiErrorHttpResponse>(StatusCodes.Status429TooManyRequests)]
    public Task<IActionResult> GetById(Guid id) =>
        HandleOk(new GetInboundMailMessageDetailsQuery(id), static value => value.ToHttpResponse());

    [HttpPost("{id:guid}/read")]
    [ServiceFilter(typeof(MailInboxMessageMetadataConcurrencyFilter))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<MailInboxApiErrorHttpResponse>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<MailInboxApiErrorHttpResponse>(StatusCodes.Status503ServiceUnavailable)]
    public Task<IActionResult> MarkRead(Guid id) =>
        ExecuteMetadataOperationAsync(
            cancellationToken => HandleNoContent(
                new MarkInboundMailMessageReadCommand(id),
                cancellationToken));

    private async Task<IActionResult> ExecuteMetadataOperationAsync(
        Func<CancellationToken, Task<IActionResult>> operation) {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(HttpContext.RequestAborted);
        timeoutSource.CancelAfter(options.Value.MessageMetadataExecutionTimeout);

        try {
            return await operation(timeoutSource.Token);
        } catch (OperationCanceledException) when (!HttpContext.RequestAborted.IsCancellationRequested) {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new MailInboxApiErrorHttpResponse(
                    "MailInbox.MessageMetadataTimedOut",
                    "Message metadata operation timed out.",
                    HttpContext.TraceIdentifier));
        }
    }
}
