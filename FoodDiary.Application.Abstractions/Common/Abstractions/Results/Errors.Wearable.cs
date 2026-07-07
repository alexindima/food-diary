using FoodDiary.Application.Abstractions.Wearables.Common;

namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class Wearable {
        public static Error InvalidProvider(string provider) => WearableErrors.InvalidProvider(provider);

        public static Error ProviderNotConfigured(string provider) => WearableErrors.ProviderNotConfigured(provider);

        public static Error NotConnected(string provider) => WearableErrors.NotConnected(provider);

        public static Error AuthFailed(string provider) => WearableErrors.AuthFailed(provider);

        public static Error InvalidState => WearableErrors.InvalidState;
    }
}
