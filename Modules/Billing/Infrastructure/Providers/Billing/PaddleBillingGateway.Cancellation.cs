using System.Text.Json.Serialization;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Results;
using FoodDiary.Modules.Billing.Domain.Contracts;

namespace FoodDiary.Modules.Billing.Infrastructure.Providers.Billing;

public sealed partial class PaddleBillingGateway {
    public async Task<Result<BillingCancellationSnapshotModel>> CancelSubscriptionAsync(
        BillingCancellationRequestModel request, CancellationToken cancellationToken = default) {
        if (!IsConfiguredForPortal()) {
            return Result.Failure<BillingCancellationSnapshotModel>(BillingErrors.ProviderNotConfigured(Provider));
        }
        // Neither a browser-supplied provider identifier nor a configured catalogue price authorizes cancellation.
        string path = $"subscriptions/{Uri.EscapeDataString(request.ExternalSubscriptionId)}";
        try {
            Result<CancellationSubscription> before = await _apiClient.SendAsync<CancellationSubscription>(
                HttpMethod.Get, path, body: null, cancellationToken);
            if (before.IsFailure) {
                return Result.Failure<BillingCancellationSnapshotModel>(before.Error);
            }
            if (!MatchesCancellationOwner(before.Value, request)) {
                return CancellationFailure("Paddle subscription ownership could not be verified.");
            }
            Result<BillingCancellationSnapshotModel>? existing = ConfirmCancellation(before.Value);
            if (existing is not null) {
                return existing;
            }
            if (before.Value.Status is not ("active" or "trialing" or "past_due" or "paused")) {
                return Result.Failure<BillingCancellationSnapshotModel>(BillingErrors.CancellationUnavailable);
            }
            string effectiveFrom = before.Value.Status is "paused" or "past_due" ? "immediately" : "next_billing_period";
            Result<CancellationSubscription> response;
            try {
                response = await _apiClient.SendAsync<CancellationSubscription>(HttpMethod.Post, path + "/cancel",
                    new { effective_from = effectiveFrom }, cancellationToken);
                if (response.IsSuccess && MatchesCancellationOwner(response.Value, request) &&
                    ConfirmCancellation(response.Value) is { IsSuccess: true } confirmed) {
                    return confirmed;
                }
            } catch (Exception exception) when (IsAmbiguousNetworkFailure(exception, cancellationToken)) {
                // A timeout is not proof of failure: Paddle may have already accepted the cancellation.
            }
            Result<CancellationSubscription> recovered = await _apiClient.SendAsync<CancellationSubscription>(
                HttpMethod.Get, path, body: null, cancellationToken);
            if (recovered.IsSuccess && MatchesCancellationOwner(recovered.Value, request) &&
                ConfirmCancellation(recovered.Value) is { IsSuccess: true } recoveredConfirmation) {
                return recoveredConfirmation;
            }
            return CancellationFailure("Paddle did not confirm subscription cancellation. Refresh and try again.");
        } catch (Exception exception) when (IsAmbiguousNetworkFailure(exception, cancellationToken)) {
            return CancellationFailure("Paddle request could not be completed. Refresh and try again.");
        }
    }

    private static bool MatchesCancellationOwner(CancellationSubscription subscription, BillingCancellationRequestModel request) =>
        string.Equals(subscription.Id, request.ExternalSubscriptionId, StringComparison.Ordinal) &&
        string.Equals(subscription.CustomerId, request.ExternalCustomerId, StringComparison.Ordinal);

    private static Result<BillingCancellationSnapshotModel>? ConfirmCancellation(CancellationSubscription subscription) {
        bool canceled = subscription.Status is "canceled";
        bool scheduled = subscription.ScheduledChange?.Action is "cancel";
        if (!canceled && !scheduled) {
            return null;
        }
        DateTimeOffset? effective = canceled ? subscription.CanceledAt : subscription.ScheduledChange?.EffectiveAt;
        if (subscription.UpdatedAt is not { } updated || effective is null ||
            (!canceled && (subscription.CurrentBillingPeriod?.EndsAt != effective ||
                subscription.Status is not ("active" or "trialing" or "past_due"))) ||
            (subscription.CurrentBillingPeriod is { StartsAt: { } start, EndsAt: { } end } && end < start)) {
            return CancellationFailure("Paddle returned incomplete cancellation dates.");
        }
        return Result.Success(new BillingCancellationSnapshotModel(
            subscription.Status!, subscription.CurrentBillingPeriod?.StartsAt?.UtcDateTime, subscription.CurrentBillingPeriod?.EndsAt?.UtcDateTime,
            !canceled && scheduled, subscription.CanceledAt?.UtcDateTime, effective.Value.UtcDateTime, updated.UtcDateTime));
    }

    private static Result<BillingCancellationSnapshotModel> CancellationFailure(string reason) =>
        Result.Failure<BillingCancellationSnapshotModel>(BillingErrors.ProviderOperationFailed(BillingProviderNames.Paddle, reason));

    private sealed record CancellationSubscription(
        string? Id,
        [property: JsonPropertyName("customer_id")] string? CustomerId,
        string? Status,
        [property: JsonPropertyName("current_billing_period")] CancellationPeriod? CurrentBillingPeriod,
        [property: JsonPropertyName("scheduled_change")] CancellationScheduledChange? ScheduledChange,
        [property: JsonPropertyName("canceled_at")] DateTimeOffset? CanceledAt,
        [property: JsonPropertyName("updated_at")] DateTimeOffset? UpdatedAt);
    private sealed record CancellationPeriod(
        [property: JsonPropertyName("starts_at")] DateTimeOffset? StartsAt,
        [property: JsonPropertyName("ends_at")] DateTimeOffset? EndsAt);
    private sealed record CancellationScheduledChange(
        string? Action, [property: JsonPropertyName("effective_at")] DateTimeOffset? EffectiveAt);
}
