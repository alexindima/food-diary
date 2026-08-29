using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Fasting.Contracts.Read.Models;
using FoodDiary.Modules.Fasting.Application.Services;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Fasting.Application.Queries.GetFastingStats;

public sealed class GetFastingStatsQueryHandler(
    IFastingAnalyticsService fastingAnalyticsService,
    ICurrentUserAccessService currentUserAccessService,
    TimeProvider dateTimeProvider)
    : IQueryHandler<GetFastingStatsQuery, Result<FastingStatsModel>> {
    public async Task<Result<FastingStatsModel>> Handle(
        GetFastingStatsQuery query, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<FastingStatsModel>(userIdResult);
        }

        FastingStatsModel stats = await fastingAnalyticsService
            .GetStatsAsync(userIdResult.Value, dateTimeProvider.GetUtcNow().UtcDateTime, cancellationToken)
            .ConfigureAwait(false);
        return Result.Success(stats);
    }
}
