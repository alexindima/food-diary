using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Billing.Application.Abstractions.Common;

public interface IBillingProviderGateway {
    string Provider { get; }

    Task<Result<BillingCancellationSnapshotModel>> CancelSubscriptionAsync(BillingCancellationRequestModel request, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Failure<BillingCancellationSnapshotModel>(BillingErrors.CancellationUnavailable));

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
