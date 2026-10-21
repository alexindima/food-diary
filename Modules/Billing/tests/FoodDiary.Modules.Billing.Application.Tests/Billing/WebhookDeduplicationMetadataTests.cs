using FoodDiary.Modules.Billing.Application.Abstractions.Common;

namespace FoodDiary.Modules.Billing.Application.Tests.Billing;

[ExcludeFromCodeCoverage]
public sealed class WebhookDeduplicationMetadataTests {
    [Fact]
    public void DuplicateWebhookFailure_PreservesBothProviderAndEventIdentity() {
        var exception = new BillingWebhookEventAlreadyProcessedException("fixture-provider", "event-123");

        Assert.Multiple(
            () => Assert.Equal("fixture-provider", exception.Provider),
            () => Assert.Equal("event-123", exception.EventId),
            () => Assert.Contains("fixture-provider", exception.Message, StringComparison.Ordinal),
            () => Assert.Contains("event-123", exception.Message, StringComparison.Ordinal));
    }
}
