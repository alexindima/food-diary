using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;

namespace FoodDiary.Application.Notifications.Common;

internal static class NotificationIdParser {
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
}
