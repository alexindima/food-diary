using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Contracts.Common.Validation;

public static class UserIdParser {
    public static Result<UserId> Parse(Guid? value) {
        return value is null || value == Guid.Empty
            ? Result.Failure<UserId>(AuthenticationErrors.InvalidToken)
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
