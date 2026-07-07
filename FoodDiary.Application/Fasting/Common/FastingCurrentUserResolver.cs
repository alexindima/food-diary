using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Fasting.Common;

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
