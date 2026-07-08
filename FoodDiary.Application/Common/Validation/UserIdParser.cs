using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Common.Validation;

public static class UserIdParser {
    public static Result<UserId> Parse(Guid? value) {
        return value is null || value == Guid.Empty
            ? Result.Failure<UserId>(Errors.Authentication.InvalidToken)
            : Result.Success(new UserId(value.Value));
    }

    public static Result<UserId> Parse(Guid value, Error emptyError) {
        return value == Guid.Empty
            ? Result.Failure<UserId>(emptyError)
            : Result.Success(new UserId(value));
    }

    public static Result ToFailure(Result<UserId> userIdResult) =>
        Result.Failure(userIdResult.Error);

    public static Result<T> ToFailure<T>(Result<UserId> userIdResult) =>
        Result.Failure<T>(userIdResult.Error);
}
