using FoodDiary.Results;
using FoodDiary.Application.Fasting.Models;

namespace FoodDiary.Application.Fasting.Common;

public interface IFastingTelemetrySummaryReadService {
    Task<Result<FastingTelemetrySummaryModel>> GetAsync(int hours, CancellationToken cancellationToken);
}
