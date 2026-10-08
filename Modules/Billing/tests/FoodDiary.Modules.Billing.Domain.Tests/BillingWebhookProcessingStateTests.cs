using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Modules.Billing.Domain.Enums;

namespace FoodDiary.Modules.Billing.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class BillingWebhookProcessingStateTests {
    private static readonly DateTime Now = new(2026, 10, 8, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Lifecycle_UsesTypedStateAndRetainsPersistedCodesAndRetryFence() {
        BillingWebhookEvent webhook = CreateReceived();
        Assert.Equal(BillingWebhookProcessingState.Received, webhook.ProcessingState);
        Assert.Equal("received", webhook.Status);

        webhook.MarkFailed(Now.AddMinutes(1), "temporary");
        Assert.Multiple(() => {
            Assert.Equal(BillingWebhookProcessingState.Failed, webhook.ProcessingState);
            Assert.Equal("failed", webhook.Status);
            Assert.Equal(1, webhook.AttemptCount);
            Assert.Equal(Now.AddMinutes(2), webhook.NextAttemptAtUtc);
        });
        webhook.MarkProcessed(Now.AddMinutes(2));
        DateTime? stamp = webhook.ModifiedOnUtc;
        webhook.MarkProcessed(DateTime.SpecifyKind(Now, DateTimeKind.Unspecified));
        Assert.Throws<InvalidOperationException>(() => webhook.MarkFailed(Now.AddMinutes(3), "late"));

        Assert.Multiple(() => {
            Assert.Equal(BillingWebhookProcessingState.Processed, webhook.ProcessingState);
            Assert.Equal("processed", webhook.Status);
            Assert.Equal(stamp, webhook.ModifiedOnUtc);
            Assert.Null(webhook.NextAttemptAtUtc);
            Assert.Null(webhook.ErrorMessage);
            Assert.Equal(1, webhook.AttemptCount);
        });
    }

    [Theory]
    [InlineData("received", BillingWebhookProcessingState.Received)]
    [InlineData("failed", BillingWebhookProcessingState.Failed)]
    [InlineData("processed", BillingWebhookProcessingState.Processed)]
    [InlineData("Processed", BillingWebhookProcessingState.Unrecognized)]
    [InlineData("future-provider-value", BillingWebhookProcessingState.Unrecognized)]
    [InlineData("", BillingWebhookProcessingState.Unrecognized)]
    public void StoredStatus_IsClassifiedExactlyWithoutChangingTheRawValue(string status, BillingWebhookProcessingState expected) {
        BillingWebhookEvent webhook = CreateReceived();
        typeof(BillingWebhookEvent).GetProperty(nameof(BillingWebhookEvent.Status))!.SetValue(webhook, status);

        Assert.Equal(expected, webhook.ProcessingState);
        Assert.Equal(status, webhook.Status);
        Assert.Null(webhook.ModifiedOnUtc);
    }

    [Fact]
    public void UnrecognizedStoredState_RetainsThePreviousNonterminalTransitionBehavior() {
        BillingWebhookEvent webhook = CreateReceived();
        typeof(BillingWebhookEvent).GetProperty(nameof(BillingWebhookEvent.Status))!.SetValue(webhook, "legacy-value");

        webhook.MarkFailed(Now.AddMinutes(1), "retry");
        webhook.MarkProcessed(Now.AddMinutes(2));

        Assert.Equal(BillingWebhookProcessingState.Processed, webhook.ProcessingState);
        Assert.Equal("processed", webhook.Status);
        Assert.Equal(1, webhook.AttemptCount);
    }

    [Fact]
    public void InvalidChronology_DoesNotPartiallyChangeTypedStateOrFailureFields() {
        BillingWebhookEvent webhook = CreateReceived();
        webhook.MarkFailed(Now.AddMinutes(2), "retry");

        Assert.Throws<ArgumentOutOfRangeException>(() => webhook.MarkProcessed(Now.AddMinutes(1)));

        Assert.Multiple(() => {
            Assert.Equal(BillingWebhookProcessingState.Failed, webhook.ProcessingState);
            Assert.Equal("failed", webhook.Status);
            Assert.Equal("retry", webhook.ErrorMessage);
            Assert.Equal(Now.AddMinutes(3), webhook.NextAttemptAtUtc);
            Assert.Null(webhook.ProcessedAtUtc);
        });
    }

    private static BillingWebhookEvent CreateReceived() => BillingWebhookEvent.CreateReceived(
        BillingProviderNames.Stripe, "evt_typed", "invoice.paid", externalObjectId: null, Now, "{}", "{}");
}
