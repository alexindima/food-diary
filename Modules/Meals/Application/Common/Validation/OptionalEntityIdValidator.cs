using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;

namespace FoodDiary.Application.Meals.Common.Validation;

internal static class OptionalEntityIdValidator {
    public static Result<TId?> Parse<TId>(Guid? value, string fieldName, string displayName, Func<Guid, TId> createId)
        where TId : struct =>
        value == Guid.Empty
            ? Result.Failure<TId?>(Errors.Validation.Invalid(fieldName, $"{displayName} must not be empty."))
            : Result.Success(value.HasValue ? createId(value.Value) : (TId?)null);
}
