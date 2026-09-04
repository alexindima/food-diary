using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class FeatureErrorContractTests {
    [Fact]
    public void BillingErrors_HasOwnerAssemblyAndNamespace() {
        Assert.Multiple(
            () => Assert.Equal("FoodDiary.Modules.Billing.Application.Abstractions", typeof(BillingErrors).Assembly.GetName().Name),
            () => Assert.Equal("FoodDiary.Application.Abstractions.Billing.Common", typeof(BillingErrors).Namespace));
    }

    [Fact]
    public void BillingErrors_PreservesEveryPublicErrorContract() {
        AssertError(BillingErrors.InvalidPlan, "Billing.InvalidPlan", "Billing plan is invalid.", ErrorKind.Validation);
        AssertError(BillingErrors.InvalidProvider("sample"), "Billing.InvalidProvider", "'sample' is not a valid billing provider.", ErrorKind.Validation);
        AssertError(BillingErrors.ProviderNotConfigured("sample"), "Billing.ProviderNotConfigured", "Billing provider 'sample' is not configured.", ErrorKind.ExternalFailure);
        AssertError(BillingErrors.ProviderOperationFailed("sample", "sample"), "Billing.ProviderOperationFailed", "Billing provider 'sample' request failed: sample", ErrorKind.ExternalFailure);
        AssertError(BillingErrors.SubscriptionAlreadyActive, "Billing.SubscriptionAlreadyActive", "Premium subscription is already active for the current user.", ErrorKind.Conflict);
        AssertError(BillingErrors.CheckoutAlreadyInProgress, "Billing.CheckoutAlreadyInProgress", "A billing checkout is already in progress for the current user.", ErrorKind.Conflict);
        AssertError(BillingErrors.TrialAlreadyUsed, "Billing.TrialAlreadyUsed", "Premium trial has already been used for the current user.", ErrorKind.Conflict);
        AssertError(BillingErrors.CustomerPortalUnavailable, "Billing.CustomerPortalUnavailable", "Billing management is not available for the current user.", ErrorKind.NotFound);
        AssertError(BillingErrors.WebhookValidationFailed("sample"), "Billing.WebhookValidationFailed", "sample", ErrorKind.Validation);
    }

    private static void AssertError(Error error, string code, string message, ErrorKind kind) {
        Assert.Multiple(
            () => Assert.Equal(code, error.Code),
            () => Assert.Equal(message, error.Message),
            () => Assert.Equal(kind, error.Kind),
            () => Assert.Null(error.Details));
    }
}
