using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.Common.Validation;

public static class DietologistRequiredIdParser {
    public static Result<TId> Parse<TId>(
        Guid value,
        string parameterName,
        string message,
        Func<Guid, TId> createId) =>
        Parse(value, Errors.Validation.Invalid(parameterName, message), createId);

    public static Result<TId> Parse<TId>(Guid value, Error emptyError, Func<Guid, TId> createId) =>
        value == Guid.Empty
            ? Result.Failure<TId>(emptyError)
            : Result.Success(createId(value));

    public static Result ToFailure<TId>(Result<TId> idResult) => Result.Failure(idResult.Error);

    public static Result<T> ToFailure<T, TId>(Result<TId> idResult) => Result.Failure<T>(idResult.Error);
}
