using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Users.Common;

internal static class CurrentUserAccessResolver {
    public static async Task<Result<UserId>> ResolveAsync(
        Guid? userId,
        ICurrentUserAccessService currentUserAccessService,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(userId);
        if (userIdResult.IsFailure) {
            return userIdResult;
        }

        Error? accessError = await currentUserAccessService
            .EnsureCanAccessAsync(userIdResult.Value, cancellationToken)
            .ConfigureAwait(false);
        return accessError is null
            ? userIdResult
            : Result.Failure<UserId>(accessError);
    }

    public static async Task<Result<UserId>> ResolveAsync(
        UserId userId,
        ICurrentUserAccessService currentUserAccessService,
        CancellationToken cancellationToken) {
        Error? accessError = await currentUserAccessService
            .EnsureCanAccessAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        return accessError is null
            ? Result.Success(userId)
            : Result.Failure<UserId>(accessError);
    }

    public static Result<T> ToFailure<T>(Result<UserId> userIdResult) =>
        UserIdParser.ToFailure<T>(userIdResult);
}
