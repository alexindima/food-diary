using FoodDiary.Application.Common.Abstractions.Result;

namespace FoodDiary.Application.Common.Validation;

public static class OptionalEntityIdValidator {
    public static Result EnsureNotEmpty(Guid? value, string fieldName, string displayName) {
        return value == Guid.Empty
            ? Result.Failure(Errors.Validation.Invalid(fieldName, $"{displayName} must not be empty."))
            : Result.Success();
    }
}
