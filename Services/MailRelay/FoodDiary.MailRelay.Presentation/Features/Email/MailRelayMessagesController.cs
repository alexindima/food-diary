using FoodDiary.MailRelay.Presentation.Controllers;
using FoodDiary.MailRelay.Presentation.Features.Email.Mappings;
using FoodDiary.MailRelay.Presentation.Features.Email.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.MailRelay.Presentation.Features.Email;

[Route("api/email/messages")]
public sealed class MailRelayMessagesController(ISender sender) : AuthorizedMailRelayController(sender) {
    [HttpGet("{id:guid}")]
    [ProducesResponseType<MailRelayMessageDetailsHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetById(Guid id) =>
        HandleOk(id.ToMessageDetailsQuery(), static value => value.ToHttpResponse());
}
