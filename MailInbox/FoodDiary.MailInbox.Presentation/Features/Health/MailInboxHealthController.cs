using FoodDiary.MailInbox.Presentation.Controllers;
using FoodDiary.MailInbox.Presentation.Features.Health.Mappings;
using FoodDiary.MailInbox.Presentation.Features.Health.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.MailInbox.Presentation.Features.Health;

[Route("")]
public sealed class MailInboxHealthController(ISender sender) : MailInboxControllerBase(sender) {
    [HttpGet("health")]
    [ProducesResponseType<MailInboxHealthHttpResponse>(StatusCodes.Status200OK)]
    public IActionResult GetHealth() => Ok(MailInboxHealthHttpMappings.ToHealthHttpResponse());

    [HttpGet("health/ready")]
    [ProducesResponseType<MailInboxHealthHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetReady() =>
        HandleOk(
            MailInboxHealthHttpMappings.ToReadinessQuery(),
            MailInboxHealthHttpMappings.ToReadyHttpResponse());
}
