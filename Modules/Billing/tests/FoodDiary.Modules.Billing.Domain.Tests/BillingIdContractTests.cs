using FoodDiary.Modules.Billing.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Billing.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class BillingIdContractTests {
    [Fact]
    public void PaymentIds_PreservePublishedValuesAcrossCopiesConversionsAndFormatting() => AssertContract(
        static value => (BillingPaymentId)value, static id => id, static (id, format) => id.ToString(format),
        static (id, value) => id with { Value = value }, BillingPaymentId.New, BillingPaymentId.Empty);

    [Fact]
    public void SubscriptionIds_PreservePublishedValuesAcrossCopiesConversionsAndFormatting() => AssertContract(
        static value => (BillingSubscriptionId)value, static id => id, static (id, format) => id.ToString(format),
        static (id, value) => id with { Value = value }, BillingSubscriptionId.New, BillingSubscriptionId.Empty);

    [Fact]
    public void WebhookIds_PreservePublishedValuesAcrossCopiesConversionsAndFormatting() => AssertContract(
        static value => (BillingWebhookEventId)value, static id => id, static (id, format) => id.ToString(format),
        static (id, value) => id with { Value = value }, BillingWebhookEventId.New, BillingWebhookEventId.Empty);

    private static void AssertContract<TId>(Func<Guid, TId> fromGuid, Func<TId, Guid> toGuid,
        Func<TId, string?, string> format, Func<TId, Guid, TId> copy, Func<TId> create, TId empty) where TId : struct {
        var value = Guid.Parse("12345678-1234-5678-9abc-def012345678");
        var replacement = Guid.Parse("87654321-4321-8765-cba9-876543210fed");
        TId original = fromGuid(value);
        TId changed = copy(original, replacement);
        TId first = create();
        TId second = create();

        Assert.Multiple(
            () => Assert.Equal(value, toGuid(original)),
            () => Assert.Equal(replacement, toGuid(changed)),
            () => Assert.Equal(Guid.Empty, toGuid(empty)),
            () => Assert.NotEqual(Guid.Empty, toGuid(first)),
            () => Assert.NotEqual(toGuid(first), toGuid(second)),
            () => Assert.Equal("12345678-1234-5678-9abc-def012345678", original.ToString()),
            () => Assert.Equal("12345678123456789abcdef012345678", format(original, "N")),
            () => Assert.Equal("12345678-1234-5678-9abc-def012345678", format(original, arg2: null)));
    }
}
