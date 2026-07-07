using FoodDiary.Application.Abstractions.Common.Abstractions.Results;

namespace FoodDiary.Application.Common.Validation;

public static class OptionalEntityIdValidator {
    public static Result EnsureNotEmpty(Guid? value, string fieldName, string displayName) {
        return value == Guid.Empty
            ? Result.Failure(Errors.Validation.Invalid(fieldName, $"{displayName} must not be empty."))
            : Result.Success();
    }

    public static Result<TId?> Parse<TId>(
        Guid? value,
        string fieldName,
        string displayName,
        Func<Guid, TId> createId) where TId : struct {
        return value == Guid.Empty
            ? Result.Failure<TId?>(Errors.Validation.Invalid(fieldName, $"{displayName} must not be empty."))
            : Result.Success(value.HasValue ? createId(value.Value) : (TId?)null);
    }
}
