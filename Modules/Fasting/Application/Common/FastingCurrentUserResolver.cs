using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Modules.Fasting.Contracts.Read.Models;
using FoodDiary.Domain.ValueObjects.Ids;

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
