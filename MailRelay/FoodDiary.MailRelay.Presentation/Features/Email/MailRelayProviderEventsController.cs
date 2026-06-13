using FoodDiary.MailRelay.Presentation.Controllers;
using FoodDiary.MailRelay.Presentation.Features.Email.Mappings;
using FoodDiary.MailRelay.Presentation.Features.Email.Requests;
using FoodDiary.MailRelay.Presentation.Features.Email.Responses;
using FoodDiary.MailRelay.Presentation.Responses;
using FoodDiary.MailRelay.Presentation.Security;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.MailRelay.Presentation.Features.Email;

[Route("api/email/providers")]
public sealed class MailRelayProviderEventsController(
    ISender sender,
    ProviderWebhookAuthorizer providerWebhookAuthorizer) : AuthorizedMailRelayController(sender) {
    [HttpPost("aws-ses/sns")]
    [ProducesResponseType<MailRelayProviderIngestionHttpResponse>(StatusCodes.Status201Created)]
    [ProducesMailRelayApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesMailRelayApiErrorResponse(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> IngestAwsSesSns(AwsSesSnsWebhookHttpRequest request) {
        if (!await providerWebhookAuthorizer.IsAwsSesSnsAuthorizedAsync(request, HttpContext.RequestAborted).ConfigureAwait(false)) {
            return Unauthorized(new MailRelayApiErrorHttpResponse(
                "MailRelay.ProviderWebhook.Unauthorized",
                "The AWS SES SNS webhook signature is invalid.",
                HttpContext.TraceIdentifier));
        }

        return await HandleCreated(
            request,
            static value => value.ToMappedCommand(),
            "/api/email/providers/aws-ses/sns",
            static value => value.ToProviderIngestionHttpResponse()).ConfigureAwait(false);
    }

    [HttpPost("mailgun/events")]
    [ProducesResponseType<MailRelayDeliveryEventHttpResponse>(StatusCodes.Status201Created)]
    [ProducesMailRelayApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesMailRelayApiErrorResponse(StatusCodes.Status401Unauthorized)]
    public Task<IActionResult> IngestMailgun(MailgunWebhookHttpRequest request) {
        if (!providerWebhookAuthorizer.IsMailgunAuthorized(request)) {
            return Task.FromResult<IActionResult>(Unauthorized(new MailRelayApiErrorHttpResponse(
                "MailRelay.ProviderWebhook.Unauthorized",
                "The Mailgun webhook signature is invalid.",
                HttpContext.TraceIdentifier)));
        }

        return HandleCreated(
            request,
            static value => value.ToMappedCommand(),
            "/api/email/providers/mailgun/events",
            static value => value.ToHttpResponse());
    }
}
