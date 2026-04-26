using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Application.Fasting.Services;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Fasting.Queries.GetFastingInsights;

public sealed class GetFastingInsightsQueryHandler(
    IFastingOccurrenceRepository fastingOccurrenceRepository,
    IFastingAnalyticsService fastingAnalyticsService,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetFastingInsightsQuery, Result<FastingInsightsModel>> {
    public async Task<Result<FastingInsightsModel>> Handle(
        GetFastingInsightsQuery query,
        CancellationToken cancellationToken) {
        var userId = new UserId(query.UserId!.Value);
        var now = dateTimeProvider.UtcNow;
        var current = await fastingOccurrenceRepository.GetCurrentAsync(userId, cancellationToken: cancellationToken);
        return Result.Success(await fastingAnalyticsService.GetInsightsAsync(userId, now, current, cancellationToken));
    }
}
