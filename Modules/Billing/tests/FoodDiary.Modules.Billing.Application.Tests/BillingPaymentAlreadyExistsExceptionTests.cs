using FoodDiary.Modules.Billing.Application.Abstractions.Common;

namespace FoodDiary.Modules.Billing.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class BillingPaymentAlreadyExistsExceptionTests {
    [Fact]
    public void BillingPaymentAlreadyExistsException_StoresPaymentIdentityAndMessage() {
        var exception = new BillingPaymentAlreadyExistsException("stripe", "payment-123");

        Assert.Equal("stripe", exception.Provider);
        Assert.Equal("payment-123", exception.ExternalPaymentId);
        Assert.Equal("Billing payment 'payment-123' for provider 'stripe' already exists.", exception.Message);
    }
}
