using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
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
}
