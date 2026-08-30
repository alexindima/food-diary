using FoodDiary.Application.Abstractions.Usda.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.Usda.Common;

public interface IUsdaDailyMicronutrientReadService {
    Task<Result<DailyMicronutrientSummaryModel>> GetDailySummaryAsync(
        UserId userId,
        DateTime date,
        CancellationToken cancellationToken);
}
