using FoodDiary.MailRelay.Presentation.Controllers;
using FoodDiary.MailRelay.Presentation.Features.Email.Mappings;
using FoodDiary.MailRelay.Presentation.Features.Email.Requests;
using FoodDiary.MailRelay.Presentation.Features.Email.Responses;
using FoodDiary.MailRelay.Presentation.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.MailRelay.Presentation.Features.Email;

[Route("api/email/events")]
public sealed class MailRelayDeliveryEventsController(ISender sender) : AuthorizedMailRelayController(sender) {
    [HttpGet]
    [ProducesResponseType<MailRelayPageHttpResponse<MailRelayDeliveryEventHttpResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetPage([FromQuery] GetMailRelayCollectionHttpQuery query) =>
        HandleOk(query.ToDeliveryEventsQuery(), static value => new MailRelayPageHttpResponse<MailRelayDeliveryEventHttpResponse>(
            value.Data.Select(item => item.ToHttpResponse()).ToArray(), value.Page, value.Limit, value.TotalPages, value.TotalItems));

    [HttpPost]
    [ProducesResponseType<MailRelayDeliveryEventHttpResponse>(StatusCodes.Status201Created)]
    [ProducesMailRelayApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Ingest(IngestMailRelayDeliveryEventHttpRequest request) =>
        HandleCreated(
            request.ToCommand(),
            static deliveryEvent => $"/api/email/events?email={Uri.EscapeDataString(deliveryEvent.Email)}",
            static deliveryEvent => deliveryEvent.ToHttpResponse());
}
