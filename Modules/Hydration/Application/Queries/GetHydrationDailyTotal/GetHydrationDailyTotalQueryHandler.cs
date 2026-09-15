using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Mediator;
using FoodDiary.Modules.Hydration.Contracts.Queries.ReadHydrationDailyTotal;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Hydration.Application.Internal;
using FoodDiary.Modules.Hydration.Contracts.Models;

namespace FoodDiary.Modules.Hydration.Application.Queries.GetHydrationDailyTotal;

public sealed class GetHydrationDailyTotalQueryHandler(
    ISender sender,
    IUserHydrationProfileReadService userProfileReadService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetHydrationDailyTotalQuery, Result<HydrationDailyModel>> {
    public async Task<Result<HydrationDailyModel>> Handle(
        GetHydrationDailyTotalQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<HydrationDailyModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        Result<double?> goalResult = await GetCurrentGoalAsync(userId, cancellationToken).ConfigureAwait(false);
        if (goalResult.IsFailure) {
            return Result.Failure<HydrationDailyModel>(goalResult.Error);
        }

        DateTime dateUtc = UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(query.DateUtc);
        int total = await sender.Send(new ReadHydrationDailyTotalQuery(userId, dateUtc), cancellationToken).ConfigureAwait(false);

        var response = new HydrationDailyModel(dateUtc, total, goalResult.Value);
        return Result.Success(response);
    }

    private async Task<Result<double?>> GetCurrentGoalAsync(UserId userId, CancellationToken cancellationToken = default) {
        Result<UserHydrationProfileModel> profileResult = await userProfileReadService
            .GetHydrationProfileAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        if (profileResult.IsFailure) {
            return Result.Failure<double?>(profileResult.Error);
        }

        return Result.Success(profileResult.Value.EffectiveWaterGoal);
    }
}
