using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Admin.Mappings;
using FoodDiary.Presentation.Api.Features.Admin.Requests;
using FoodDiary.Presentation.Api.Features.Admin.Responses;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Admin;

[ApiController]
[Route("api/v{version:apiVersion}/admin/billing")]
[Authorize(Roles = PresentationRoleNames.Admin)]
public sealed class AdminBillingController(ISender mediator) : BaseApiController(mediator) {
    [HttpGet("subscriptions")]
    [ProducesResponseType<PagedHttpResponse<AdminBillingSubscriptionHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetSubscriptions([FromQuery] GetAdminBillingHttpQuery query) =>
        HandleOk(query.ToSubscriptionsQuery(), static value => value.ToBillingSubscriptionsHttpResponse());

    [HttpGet("payments")]
    [ProducesResponseType<PagedHttpResponse<AdminBillingPaymentHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetPayments([FromQuery] GetAdminBillingHttpQuery query) =>
        HandleOk(query.ToPaymentsQuery(), static value => value.ToBillingPaymentsHttpResponse());

    [HttpGet("webhook-events")]
    [ProducesResponseType<PagedHttpResponse<AdminBillingWebhookEventHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetWebhookEvents([FromQuery] GetAdminBillingHttpQuery query) =>
        HandleOk(query.ToWebhookEventsQuery(), static value => value.ToBillingWebhookEventsHttpResponse());
}
