using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Users.Common;

internal static class UserInputParser {
    public static Result<TEnum?> ParseOptionalEnum<TEnum>(string? value, string fieldName, string message)
        where TEnum : struct, Enum {
        if (string.IsNullOrWhiteSpace(value)) {
            return Result.Success<TEnum?>(value: null);
        }

        return Enum.TryParse(value, ignoreCase: true, out TEnum parsed)
            ? Result.Success<TEnum?>(parsed)
            : Result.Failure<TEnum?>(Errors.Validation.Invalid(fieldName, message));
    }

    public static Result<ImageAssetId?> ParseOptionalImageAssetId(Guid? value, string fieldName) {
        if (!value.HasValue) {
            return Result.Success<ImageAssetId?>(value: null);
        }

        return value.Value == Guid.Empty
            ? Result.Failure<ImageAssetId?>(Errors.Validation.Invalid(fieldName, "Image asset id must not be empty."))
            : Result.Success<ImageAssetId?>(new ImageAssetId(value.Value));
    }
}
