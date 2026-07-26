using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Billing.Common;

internal static class BillingCurrentUserAccessResolver {
    public static async Task<Result<UserId>> ResolveAsync(
        Guid? userId,
        ICurrentUserAccessService currentUserAccessService,
        CancellationToken cancellationToken) {
        if (!userId.HasValue || userId.Value == Guid.Empty) {
            return Result.Failure<UserId>(Errors.Authentication.InvalidToken);
        }

        var parsedUserId = new UserId(userId.Value);
        Error? accessError = await currentUserAccessService
            .EnsureCanAccessAsync(parsedUserId, cancellationToken)
            .ConfigureAwait(false);
        return accessError is null
            ? Result.Success(parsedUserId)
            : Result.Failure<UserId>(accessError);
    }

    public static Result<T> ToFailure<T>(Result<UserId> userIdResult) =>
        Result.Failure<T>(userIdResult.Error);
}
