using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Wearables.Common;

internal static class WearableProviderParser {
    public static Result<WearableProvider> Parse(string value) {
        WearableProvider? provider = Enum.GetValues<WearableProvider>()
            .Cast<WearableProvider?>()
            .FirstOrDefault(candidate => string.Equals(candidate.ToString(), value, StringComparison.OrdinalIgnoreCase));

        return provider.HasValue
            ? Result.Success(provider.Value)
            : Result.Failure<WearableProvider>(Errors.Wearable.InvalidProvider(value));
    }
}
