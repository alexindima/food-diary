using FoodDiary.Application.Billing.Models;
using FoodDiary.Application.Common.Abstractions.Result;

namespace FoodDiary.Application.Billing.Common;

public interface IBillingProviderGateway {
    string Provider { get; }

    Task<Result<BillingCheckoutSessionModel>> CreateCheckoutSessionAsync(
        BillingCheckoutSessionRequestModel request,
        CancellationToken cancellationToken = default);

    Task<Result<BillingPortalSessionModel>> CreatePortalSessionAsync(
        BillingPortalSessionRequestModel request,
        CancellationToken cancellationToken = default);

    Task<Result<BillingWebhookEventModel?>> ParseWebhookEventAsync(
        string payload,
        string signatureHeader,
        CancellationToken cancellationToken = default);
}
