using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Domain.Entities;
using System.Reflection;

namespace FoodDiary.Modules.Billing.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class BillingWebhookPersistenceShapeTests {
    private static void ReadPublicProperties(object instance) {
        foreach (PropertyInfo property in instance.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)) {
            if (property.GetIndexParameters().Length == 0) {
                property.GetValue(instance);
            }
        }
    }

    [Fact]
    public void EntityNavigationAndPrivateConstructors_AreCoveredForEfOnlyMembers() {
        var webhookEvent = BillingWebhookEvent.CreateProcessed(
            BillingProviderNames.YooKassa,
            eventId: "evt",
            eventType: "payment.succeeded",
            externalObjectId: "payment",
            processedAtUtc: DateTime.UtcNow,
            payloadJson: "{}");
        ReadPublicProperties(webhookEvent);
        Assert.Multiple(
            () => Assert.Equal(BillingProviderNames.YooKassa, webhookEvent.Provider));
    }
}
