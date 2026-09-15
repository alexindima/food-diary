using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Domain.Entities;

namespace FoodDiary.Modules.Billing.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class BillingWebhookChronologyTests {
    private static readonly DateTime Now = new(2026, 8, 19, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void BillingWebhookEvent_EnforcesMonotonicTerminalTransitions() {
        var webhookEvent = BillingWebhookEvent.CreateReceived(
            BillingProviderNames.Stripe,
            "evt_1",
            "invoice.paid",
            externalObjectId: null,
            Now,
            "{}",
            "{}");

        Assert.Throws<ArgumentOutOfRangeException>(() => webhookEvent.MarkProcessed(Now.AddTicks(-1)));
        Assert.Equal(BillingWebhookEvent.ReceivedStatus, webhookEvent.Status);

        webhookEvent.MarkFailed(Now.AddMinutes(1), "temporary");
        Assert.Throws<ArgumentOutOfRangeException>(() => webhookEvent.MarkProcessed(Now.AddSeconds(30)));
        Assert.Equal(BillingWebhookEvent.FailedStatus, webhookEvent.Status);

        webhookEvent.MarkProcessed(Now.AddMinutes(2));
        DateTime? modifiedAfterProcessing = webhookEvent.ModifiedOnUtc;
        webhookEvent.MarkProcessed(Now.AddMinutes(3));
        Assert.Throws<InvalidOperationException>(() => webhookEvent.MarkFailed(Now.AddMinutes(3), "late failure"));
        Assert.Equal(BillingWebhookEvent.ProcessedStatus, webhookEvent.Status);
        Assert.Equal(modifiedAfterProcessing, webhookEvent.ModifiedOnUtc);
    }
}
