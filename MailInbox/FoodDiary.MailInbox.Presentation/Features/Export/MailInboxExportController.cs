using System.ComponentModel.DataAnnotations;
using FoodDiary.MailInbox.Application.Abstractions;
using FoodDiary.MailInbox.Application.Messages.Models;
using FoodDiary.MailInbox.Presentation.Filters;
using FoodDiary.MailInbox.Presentation.Security;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.MailInbox.Presentation.Features.Export;

[ApiController]
[Route("api/mail-inbox/export")]
[ServiceFilter(typeof(MailInboxApiKeyAuthorizationFilter))]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class MailInboxExportController(IInboundMailExportStore store) : ControllerBase {
    [HttpGet]
    [RequireMailInboxPermission(MailInboxPermission.Metadata)]
    [ServiceFilter(typeof(MailInboxMessageMetadataConcurrencyFilter))]
    public async Task<ActionResult<IReadOnlyList<InboundMailExportEntryHttpResponse>>> GetAsync(
        [Required, EmailAddress, StringLength(320)] string recipient,
        DateTimeOffset? beforeReceivedAtUtc, Guid? beforeId,
        [Range(1, 100)] int limit = 50, CancellationToken cancellationToken = default) {
        if (beforeReceivedAtUtc.HasValue != beforeId.HasValue) {
            return BadRequest("Both cursor fields must be supplied together.");
        }
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        IReadOnlyList<InboundMailExportEntry> page = await store.GetExportPageAsync(recipient, beforeReceivedAtUtc, beforeId, limit, timeout.Token);
        return Ok(page.Select(static entry => new InboundMailExportEntryHttpResponse(entry.Id, entry.ReceivedAtUtc, entry.ContentAvailable)).ToArray());
    }

    [HttpGet("{id:guid}/mime")]
    [RequireMailInboxPermission(MailInboxPermission.Content)]
    [ServiceFilter(typeof(MailInboxMessageDetailConcurrencyFilter))]
    public async Task<IActionResult> GetMimeAsync(Guid id,
        [Required, EmailAddress, StringLength(320)] string recipient, CancellationToken cancellationToken) {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        byte[]? mime = await store.GetRawMimeAsync(id, recipient, timeout.Token);
        return mime is null ? NotFound() : File(mime, "application/octet-stream", $"{id}.eml");
    }
}
