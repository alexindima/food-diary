using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Fasting.Models;

namespace FoodDiary.Application.Abstractions.Fasting.Common;

public interface IFastingTelemetrySummaryReadService {
    Task<Result<FastingTelemetrySummaryModel>> GetAsync(int hours, CancellationToken cancellationToken);
}
