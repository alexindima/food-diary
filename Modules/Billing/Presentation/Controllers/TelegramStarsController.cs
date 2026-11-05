using FoodDiary.Mediator;
using FoodDiary.Modules.Billing.Presentation.Mappings;
using FoodDiary.Modules.Billing.Presentation.Requests;
using FoodDiary.Modules.Billing.Presentation.Responses;
using FoodDiary.Modules.Billing.Presentation.Security;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FoodDiary.Modules.Billing.Presentation.Controllers;

[ApiController]
[AllowAnonymous]
[ServiceFilter(typeof(TelegramStarsBotAuthorizationFilter))]
[EnableRateLimiting(PresentationPolicyNames.WebhookRateLimitPolicyName)]
[Route("api/v{version:apiVersion}/billing/telegram-stars/updates")]
public sealed class TelegramStarsController(ISender sender) : BaseApiController(sender) {
    [HttpPost]
    [RequestSizeLimit(8192)]
    [ProducesResponseType<TelegramStarsUpdateHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status401Unauthorized)]
    public Task<IActionResult> ProcessUpdate([FromBody] TelegramStarsUpdateHttpRequest request) =>
        HandleOk(request.ToCommand(), static result => new TelegramStarsUpdateHttpResponse(result.CheckoutApproved, result.Error));
}
