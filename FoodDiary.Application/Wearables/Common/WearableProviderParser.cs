using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Wearables.Common;

internal static class WearableProviderParser {
    public static Result<WearableProvider> Parse(string value) =>
        EnumValueParser.ParseRequired<WearableProvider>(value, Errors.Wearable.InvalidProvider(value));
}
