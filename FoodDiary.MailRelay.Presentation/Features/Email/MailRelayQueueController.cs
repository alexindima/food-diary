using FoodDiary.MailRelay.Client.Models;
using FoodDiary.MailRelay.Presentation.Controllers;
using FoodDiary.MailRelay.Presentation.Features.Email.Mappings;
using FoodDiary.MailRelay.Presentation.Features.Email.Responses;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.MailRelay.Presentation.Features.Email;

[Route("api/email")]
public sealed class MailRelayQueueController(ISender sender) : AuthorizedMailRelayController(sender) {
    [HttpGet("queue/stats")]
    [ProducesResponseType<MailRelayQueueStatsHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetStats() =>
        HandleOk(MailRelayEmailHttpMappings.ToQueueStatsQuery(), static value => value.ToHttpResponse());

    [HttpPost("send")]
    [ProducesResponseType<EnqueueMailRelayEmailResponse>(StatusCodes.Status202Accepted)]
    public Task<IActionResult> Enqueue(EnqueueMailRelayEmailRequest request) =>
        HandleAccepted(
            request.ToCommand(),
            static queuedEmailId => $"/api/email/messages/{queuedEmailId}",
            static queuedEmailId => queuedEmailId.ToEnqueuedHttpResponse());
}
