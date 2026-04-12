using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Application.Fasting.Services;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Fasting.Queries.GetFastingStats;

public class GetFastingStatsQueryHandler(
    IFastingAnalyticsService fastingAnalyticsService,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetFastingStatsQuery, Result<FastingStatsModel>> {
    public async Task<Result<FastingStatsModel>> Handle(
        GetFastingStatsQuery query, CancellationToken cancellationToken) {
        var userId = new UserId(query.UserId!.Value);
        return Result.Success(await fastingAnalyticsService.GetStatsAsync(userId, dateTimeProvider.UtcNow, cancellationToken));
    }
}
