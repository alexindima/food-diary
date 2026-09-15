using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Domain.Entities;
using System.Reflection;

namespace FoodDiary.Modules.Billing.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class BillingWebhookRetryBoundaryTests {
    private static readonly DateTime Now = new(2026, 8, 19, 12, 0, 0, DateTimeKind.Utc);

    private static BillingWebhookEvent CreateReceivedWebhookEvent() => BillingWebhookEvent.CreateReceived(
        BillingProviderNames.Stripe,
        Guid.NewGuid().ToString("N"),
        "payment.failed",
        externalObjectId: null,
        Now,
        "{}",
        "{}");

    [Fact]
    public void BoundaryTransitions_HandleOverflowWithoutPartialMutation() {
        BillingWebhookEvent webhookEvent = CreateReceivedWebhookEvent();
        var boundary = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
        webhookEvent.MarkFailed(boundary, "failure");
        Assert.Multiple(
            () => Assert.Equal(BillingWebhookEvent.FailedStatus, webhookEvent.Status),
            () => Assert.Equal(1, webhookEvent.AttemptCount),
            () => Assert.Equal("failure", webhookEvent.ErrorMessage),
            () => Assert.Equal(boundary, webhookEvent.NextAttemptAtUtc),
            () => Assert.Equal(boundary, webhookEvent.ModifiedOnUtc));
        BillingWebhookEvent saturatedEvent = CreateReceivedWebhookEvent();
        PropertyInfo attemptCountProperty = typeof(BillingWebhookEvent)
            .GetProperty(nameof(BillingWebhookEvent.AttemptCount))!;
        attemptCountProperty.SetValue(saturatedEvent, int.MaxValue);
        saturatedEvent.MarkFailed(Now.AddMinutes(1), "failure");
        Assert.Equal(int.MaxValue, saturatedEvent.AttemptCount);
    }
}
