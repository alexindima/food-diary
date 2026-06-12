using FoodDiary.MailRelay.Presentation.Controllers;
using FoodDiary.MailRelay.Presentation.Features.Health.Mappings;
using FoodDiary.MailRelay.Presentation.Features.Health.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.MailRelay.Presentation.Features.Health;

[Route("")]
public sealed class MailRelayHealthController(ISender sender) : MailRelayControllerBase(sender) {
    [HttpGet("health")]
    [ProducesResponseType<MailRelayHealthHttpResponse>(StatusCodes.Status200OK)]
    public IActionResult GetHealth() => Ok(MailRelayHealthHttpMappings.ToHealthHttpResponse());

    [HttpGet("health/ready")]
    [ProducesResponseType<MailRelayHealthHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetReady() =>
        HandleOk(
            MailRelayHealthHttpMappings.ToReadinessQuery(),
            MailRelayHealthHttpMappings.ToReadyHttpResponse());
}
