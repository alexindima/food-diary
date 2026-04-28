using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Billing.Mappings;
using FoodDiary.Presentation.Api.Features.Billing.Requests;
using FoodDiary.Presentation.Api.Features.Billing.Responses;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Presentation.Api.Security;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Billing;

[ApiController]
[Route("api/v{version:apiVersion}/billing")]
[Authorize]
public sealed class BillingController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet("overview")]
    [ProducesResponseType<BillingOverviewHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetOverview([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToBillingOverviewQuery(), static value => value.ToHttpResponse());

    [HttpPost("checkout-session")]
    [ProducesResponseType<CheckoutSessionHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    [BlockImpersonatedAccess]
    public Task<IActionResult> CreateCheckoutSession(
        [FromCurrentUser] Guid userId,
        [FromBody] CreateCheckoutSessionHttpRequest request) =>
        HandleOk(request.ToCommand(userId), static value => value.ToHttpResponse());

    [HttpPost("portal-session")]
    [ProducesResponseType<PortalSessionHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [BlockImpersonatedAccess]
    public Task<IActionResult> CreatePortalSession([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToPortalSessionCommand(), static value => value.ToHttpResponse());
}
