using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Application.Fasting.Services;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Fasting.Queries.GetFastingHistory;

public class GetFastingHistoryQueryHandler(
    IFastingAnalyticsService fastingAnalyticsService)
    : IQueryHandler<GetFastingHistoryQuery, Result<PagedResponse<FastingSessionModel>>> {
    public async Task<Result<PagedResponse<FastingSessionModel>>> Handle(
        GetFastingHistoryQuery query, CancellationToken cancellationToken) {
        var userId = new UserId(query.UserId!.Value);
        return Result.Success(await fastingAnalyticsService.GetHistoryAsync(
            userId,
            query.Page,
            query.Limit,
            query.From,
            query.To,
            cancellationToken));
    }
}
