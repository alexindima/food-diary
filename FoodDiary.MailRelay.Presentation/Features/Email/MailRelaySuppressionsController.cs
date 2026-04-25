using FoodDiary.MailRelay.Presentation.Controllers;
using FoodDiary.MailRelay.Presentation.Features.Email.Mappings;
using FoodDiary.MailRelay.Presentation.Features.Email.Requests;
using FoodDiary.MailRelay.Presentation.Features.Email.Responses;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.MailRelay.Presentation.Features.Email;

[Route("api/email/suppressions")]
public sealed class MailRelaySuppressionsController(ISender sender) : AuthorizedMailRelayController(sender) {
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<MailRelaySuppressionHttpResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> Get([FromQuery] string? email) =>
        HandleOk(email.ToSuppressionsQuery(), static value => value.ToHttpResponse());

    [HttpPost]
    [ProducesResponseType<MailRelaySuppressionCreatedHttpResponse>(StatusCodes.Status201Created)]
    public Task<IActionResult> Create(CreateMailRelaySuppressionHttpRequest request) =>
        HandleCreated(
            request.ToCommand(),
            $"/api/email/suppressions?email={Uri.EscapeDataString(request.Email)}",
            MailRelayEmailHttpMappings.ToSuppressionCreatedHttpResponse());

    [HttpDelete("{email}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Delete(string email) =>
        HandleNoContent(email.ToRemoveSuppressionCommand());
}
