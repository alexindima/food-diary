using FoodDiary.MailRelay.Presentation.Controllers;
using FoodDiary.MailRelay.Presentation.Features.Email.Mappings;
using FoodDiary.MailRelay.Presentation.Features.Email.Requests;
using FoodDiary.MailRelay.Presentation.Features.Email.Responses;
using FoodDiary.MailRelay.Presentation.Responses;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.MailRelay.Presentation.Features.Email;

[Route("api/email/events")]
public sealed class MailRelayDeliveryEventsController(ISender sender) : AuthorizedMailRelayController(sender) {
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<MailRelayDeliveryEventHttpResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> Get([FromQuery] string? email) =>
        HandleOk(email.ToDeliveryEventsQuery(), static value => value.ToHttpResponse());

    [HttpPost]
    [ProducesResponseType<MailRelayDeliveryEventHttpResponse>(StatusCodes.Status201Created)]
    [ProducesMailRelayApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Ingest(IngestMailRelayDeliveryEventHttpRequest request) =>
        HandleCreated(
            request.ToCommand(),
            static deliveryEvent => $"/api/email/events?email={Uri.EscapeDataString(deliveryEvent.Email)}",
            static deliveryEvent => deliveryEvent.ToHttpResponse());
}
