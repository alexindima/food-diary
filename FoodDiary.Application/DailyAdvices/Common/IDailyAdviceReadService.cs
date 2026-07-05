using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.DailyAdvices.Models;

namespace FoodDiary.Application.DailyAdvices.Common;

public interface IDailyAdviceReadService {
    Task<Result<DailyAdviceModel>> GetForDateAsync(
        DateTime date,
        string? locale,
        CancellationToken cancellationToken);
}
