using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Cycles.Common;

public interface ICycleReadService {
    Task<CycleModel?> GetCurrentAsync(
        UserId userId,
        CancellationToken cancellationToken);

    Task<Result<CycleNutritionSummaryModel?>> GetNutritionSummaryAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken);
}
