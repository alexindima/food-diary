using FoodDiary.Modules.Wearables.Application.Abstractions.Common;
using FoodDiary.Results;
using FoodDiary.Modules.Wearables.Domain.Enums;

namespace FoodDiary.Modules.Wearables.Application.Common;

internal static class WearableProviderParser {
    public static Result<WearableProvider> Parse(string value) {
        WearableProvider? provider = Enum.GetValues<WearableProvider>()
            .Cast<WearableProvider?>()
            .FirstOrDefault(candidate => string.Equals(candidate.ToString(), value, StringComparison.OrdinalIgnoreCase));

        return provider.HasValue
            ? Result.Success(provider.Value)
            : Result.Failure<WearableProvider>(WearableErrors.InvalidProvider(value));
    }
}
