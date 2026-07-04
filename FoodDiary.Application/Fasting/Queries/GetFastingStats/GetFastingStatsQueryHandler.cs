using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Application.Fasting.Services;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Fasting.Queries.GetFastingStats;

public sealed class GetFastingStatsQueryHandler(
    IFastingAnalyticsService fastingAnalyticsService,
    ICurrentUserAccessService currentUserAccessService,
    TimeProvider dateTimeProvider)
    : IQueryHandler<GetFastingStatsQuery, Result<FastingStatsModel>> {
    public async Task<Result<FastingStatsModel>> Handle(
        GetFastingStatsQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<FastingStatsModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId!.Value);
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        return accessError is not null ? Result.Failure<FastingStatsModel>(accessError) : Result.Success(await fastingAnalyticsService.GetStatsAsync(userId, dateTimeProvider.GetUtcNow().UtcDateTime, cancellationToken).ConfigureAwait(false));
    }
}
