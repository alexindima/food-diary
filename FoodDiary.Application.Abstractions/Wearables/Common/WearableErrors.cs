using FoodDiary.Application.Abstractions.Common.Abstractions.Results;

namespace FoodDiary.Application.Abstractions.Wearables.Common;

public static class WearableErrors {
    public static Error InvalidProvider(string provider) => new(
        "Wearable.InvalidProvider",
        $"'{provider}' is not a valid wearable provider.",
        Kind: ErrorKind.Validation);

    public static Error ProviderNotConfigured(string provider) => new(
        "Wearable.ProviderNotConfigured",
        $"Wearable provider '{provider}' is not configured.",
        Kind: ErrorKind.Internal);

    public static Error NotConnected(string provider) => new(
        "Wearable.NotConnected",
        $"No active connection found for provider '{provider}'.",
        Kind: ErrorKind.NotFound);

    public static Error AuthFailed(string provider) => new(
        "Wearable.AuthFailed",
        $"Authentication with '{provider}' failed.",
        Kind: ErrorKind.Unauthorized);

    public static Error InvalidState => new(
        "Wearable.InvalidState",
        "Wearable authentication state is invalid or expired.",
        Kind: ErrorKind.Unauthorized);
}
