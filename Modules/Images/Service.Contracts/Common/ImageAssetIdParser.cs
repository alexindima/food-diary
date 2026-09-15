using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;

namespace FoodDiary.Modules.Images.Service.Contracts.Common;

public static class ImageAssetIdParser {
    public static Result<ImageAssetId> ParseRequired(Guid value, Error requiredError) =>
        value == Guid.Empty
            ? Result.Failure<ImageAssetId>(requiredError)
            : Result.Success(new ImageAssetId(value));

    public static Result<ImageAssetId?> ParseOptional(Guid? value, string fieldName) {
        if (!value.HasValue) {
            return Result.Success<ImageAssetId?>(value: null);
        }

        return value.Value == Guid.Empty
            ? Result.Failure<ImageAssetId?>(Errors.Validation.Invalid(fieldName, "Image asset id must not be empty."))
            : Result.Success<ImageAssetId?>(new ImageAssetId(value.Value));
    }
}
