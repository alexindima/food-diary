using FoodDiary.MailRelay.Presentation.Controllers;
using FoodDiary.MailRelay.Presentation.Features.Email.Mappings;
using FoodDiary.MailRelay.Presentation.Features.Email.Requests;
using FoodDiary.MailRelay.Presentation.Features.Email.Responses;
using FoodDiary.MailRelay.Presentation.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.MailRelay.Presentation.Features.Email;

[Route("api/email/providers")]
public sealed class MailRelayProviderEventsController(ISender sender) : AuthorizedMailRelayController(sender) {
    [HttpPost("aws-ses/sns")]
    [ProducesResponseType<MailRelayProviderIngestionHttpResponse>(StatusCodes.Status201Created)]
    [ProducesMailRelayApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> IngestAwsSesSns(AwsSesSnsWebhookHttpRequest request) =>
        HandleCreated(
            request,
            static value => value.ToMappedCommand(),
            "/api/email/providers/aws-ses/sns",
            static value => value.ToProviderIngestionHttpResponse());

    [HttpPost("mailgun/events")]
    [ProducesResponseType<MailRelayDeliveryEventHttpResponse>(StatusCodes.Status201Created)]
    [ProducesMailRelayApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> IngestMailgun(MailgunWebhookHttpRequest request) =>
        HandleCreated(
            request,
            static value => value.ToMappedCommand(),
            "/api/email/providers/mailgun/events",
            static value => value.ToHttpResponse());
}
