using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Common.Validation;

public static class UserIdParser {
    public static Result<UserId> Parse(Guid? value) {
        return value is null || value == Guid.Empty
            ? Result.Failure<UserId>(Errors.Authentication.InvalidToken)
            : Result.Success(new UserId(value.Value));
    }

    public static Result ToFailure(Result<UserId> userIdResult) =>
        UserIdParser.ToFailure(userIdResult);

    public static Result<T> ToFailure<T>(Result<UserId> userIdResult) =>
        UserIdParser.ToFailure<T>(userIdResult);
}
