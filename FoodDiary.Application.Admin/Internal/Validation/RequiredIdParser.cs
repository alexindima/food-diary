using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Internal.Validation;

internal static class RequiredIdParser {
    public static Result<TId> Parse<TId>(
        Guid value,
        string parameterName,
        string message,
        Func<Guid, TId> createId) =>
        value == Guid.Empty
            ? Result.Failure<TId>(Errors.Validation.Invalid(parameterName, message))
            : Result.Success(createId(value));

    public static Result ToFailure<TId>(Result<TId> idResult) =>
        Result.Failure(idResult.Error);

    public static Result<T> ToFailure<T, TId>(Result<TId> idResult) =>
        Result.Failure<T>(idResult.Error);
}
