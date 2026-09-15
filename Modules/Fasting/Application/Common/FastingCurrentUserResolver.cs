using FoodDiary.Modules.Users.Contracts.Common.Validation;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Fasting.Contracts.Read.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Fasting.Application.Common;

internal static class FastingCurrentUserResolver {
    public static async Task<Result<UserId>> ResolveAsync(
        Guid? userId,
        ICurrentUserAccessService currentUserAccessService,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            userId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        return userIdResult.IsFailure
            ? UserIdParser.ToFailure<UserId>(userIdResult)
            : userIdResult;
    }

    public static Result<FastingSessionModel> ToSessionFailure(Result<UserId> userIdResult) =>
        UserIdParser.ToFailure<FastingSessionModel>(userIdResult);
}
