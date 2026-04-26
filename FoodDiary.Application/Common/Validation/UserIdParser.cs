using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Common.Validation;

public static class UserIdParser {
    public static Result<UserId> Parse(Guid? value) {
        return value is null || value == Guid.Empty
            ? Result.Failure<UserId>(Errors.Authentication.InvalidToken)
            : Result.Success(new UserId(value.Value));
    }
}
