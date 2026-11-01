using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Billing.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Billing.Domain.Entities;

public sealed class BillingCancellation : Entity<Guid> {
    public BillingCancellationId TypedId => new(Id);
    public UserId UserId { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string ExternalSubscriptionId { get; private set; } = string.Empty;
    public string? Plan { get; private set; }
    public string Source { get; private set; } = string.Empty;
    public string Outcome { get; private set; } = string.Empty;
    public DateTime RequestedAtUtc { get; private set; }
    public DateTime EffectiveAtUtc { get; private set; }
    public string? ReasonCode { get; private set; }
    public string? Comment { get; private set; }
    public DateTime? FeedbackSubmittedAtUtc { get; private set; }
    public DateTime? ConfirmationEmailHandledAtUtc { get; private set; }

    private BillingCancellation() { }

    public static BillingCancellation Create(BillingSubscription subscription, string source,
        DateTime requestedAtUtc, DateTime effectiveAtUtc) {
        if (subscription.ExternalSubscriptionId is null || !IsCancellation(subscription)) {
            throw new ArgumentException("A confirmed cancellation is required.", nameof(subscription));
        }
        if (source is not ("fooddiary" or "paddle")) {
            throw new ArgumentException("Unknown cancellation source.", nameof(source));
        }
        var cancellation = new BillingCancellation {
            Id = Guid.NewGuid(),
            UserId = subscription.UserId,
            Provider = subscription.Provider,
            ExternalSubscriptionId = subscription.ExternalSubscriptionId,
            Plan = subscription.Plan,
            Source = source,
            Outcome = subscription.Status is "canceled" ? "canceled" : "scheduled",
            RequestedAtUtc = RequireUtc(requestedAtUtc),
            EffectiveAtUtc = RequireUtc(effectiveAtUtc),
        };
        cancellation.SetCreated(requestedAtUtc);
        return cancellation;
    }

    public void Synchronize(BillingSubscription subscription, DateTime nowUtc) {
        if (subscription.UserId != UserId || !string.Equals(subscription.Provider, Provider, StringComparison.Ordinal) ||
            !string.Equals(subscription.ExternalSubscriptionId, ExternalSubscriptionId, StringComparison.Ordinal)) {
            throw new ArgumentException("Cancellation owner does not match.", nameof(subscription));
        }
        Outcome = subscription.Status switch {
            "canceled" => "canceled",
            _ when subscription.CancelAtPeriodEnd => "scheduled",
            _ => "cancellation_removed",
        };
        if (subscription.Status is "canceled" && subscription.CanceledAtUtc is { } canceledAt) {
            EffectiveAtUtc = RequireUtc(canceledAt);
        } else if (subscription.CancelAtPeriodEnd && subscription.CurrentPeriodEndUtc is { } end) {
            EffectiveAtUtc = RequireUtc(end);
        }
        SetModified(RequireUtc(nowUtc));
    }

    public void SubmitFeedback(string? reasonCode, string? comment, DateTime submittedAtUtc) {
        string? reason = string.IsNullOrWhiteSpace(reasonCode) ? null : reasonCode.Trim();
        string? text = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        if (reason is not null && reason is not ("too_expensive" or "ai_not_useful" or "missing_features" or
                "technical_issues" or "not_using" or "other")) {
            throw new ArgumentException("Unknown cancellation reason.", nameof(reasonCode));
        }
        if (text?.Length > 2000) {
            throw new ArgumentException("Feedback must not exceed 2000 characters.", nameof(comment));
        }
        DateTime instant = RequireUtc(submittedAtUtc);
        ReasonCode = reason;
        Comment = text;
        FeedbackSubmittedAtUtc = reason is null && text is null ? null : instant;
        SetModified(instant);
    }

    public void MarkConfirmationEmailHandled(DateTime handledAtUtc) {
        if (ConfirmationEmailHandledAtUtc is not null) {
            return;
        }
        ConfirmationEmailHandledAtUtc = RequireUtc(handledAtUtc);
        SetModified(handledAtUtc);
    }

    private static bool IsCancellation(BillingSubscription subscription) =>
        subscription.CancelAtPeriodEnd || subscription.Status is "canceled";

    private static DateTime RequireUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : throw new ArgumentException("A UTC instant is required.", nameof(value));
}
