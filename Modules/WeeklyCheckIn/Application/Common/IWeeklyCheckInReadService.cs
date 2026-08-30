using FoodDiary.Results;
using FoodDiary.Application.WeeklyCheckIn.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.WeeklyCheckIn.Common;

public interface IWeeklyCheckInReadService {
    Task<Result<WeekSummaryModel>> LoadWeekSummaryAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken);
}
