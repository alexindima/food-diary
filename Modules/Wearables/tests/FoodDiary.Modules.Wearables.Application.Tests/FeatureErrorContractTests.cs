using FoodDiary.Application.Abstractions.Wearables.Common;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class FeatureErrorContractTests {
    [Fact]
    public void WearableErrors_HasOwnerAssemblyAndNamespace() {
        Assert.Multiple(
            () => Assert.Equal("FoodDiary.Modules.Wearables.Application.Abstractions", typeof(WearableErrors).Assembly.GetName().Name),
            () => Assert.Equal("FoodDiary.Application.Abstractions.Wearables.Common", typeof(WearableErrors).Namespace));
    }

    [Fact]
    public void WearableErrors_PreservesEveryPublicErrorContract() {
        AssertError(WearableErrors.InvalidProvider("sample"), "Wearable.InvalidProvider", "'sample' is not a valid wearable provider.", ErrorKind.Validation);
        AssertError(WearableErrors.ProviderNotConfigured("sample"), "Wearable.ProviderNotConfigured", "Wearable provider 'sample' is not configured.", ErrorKind.Internal);
        AssertError(WearableErrors.NotConnected("sample"), "Wearable.NotConnected", "No active connection found for provider 'sample'.", ErrorKind.NotFound);
        AssertError(WearableErrors.AuthFailed("sample"), "Wearable.AuthFailed", "Authentication with 'sample' failed.", ErrorKind.Unauthorized);
        AssertError(WearableErrors.SyncFailed("sample"), "Wearable.SyncFailed", "Synchronizing data with 'sample' failed.", ErrorKind.ExternalFailure);
        AssertError(WearableErrors.InvalidState, "Wearable.InvalidState", "Wearable authentication state is invalid or expired.", ErrorKind.Unauthorized);
        AssertError(WearableErrors.IdempotencyConflict, "Idempotency.Conflict", "The idempotency key was already used with a different request.", ErrorKind.Conflict);
    }

    private static void AssertError(Error error, string code, string message, ErrorKind kind) {
        Assert.Multiple(
            () => Assert.Equal(code, error.Code),
            () => Assert.Equal(message, error.Message),
            () => Assert.Equal(kind, error.Kind),
            () => Assert.Null(error.Details));
    }
}
