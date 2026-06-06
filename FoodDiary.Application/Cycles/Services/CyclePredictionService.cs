using FoodDiary.Application.Cycles.Models;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.Cycles.Services;

public static class CyclePredictionService {
    private const int DefaultPmsWindow = 5;

    public static CyclePredictionsModel CalculatePredictions(Cycle cycle) {
        ArgumentNullException.ThrowIfNull(cycle);

        DateTime nextPeriodStart = NormalizeDate(cycle.StartDate.AddDays(cycle.AverageLength));
        DateTime ovulation = NormalizeDate(nextPeriodStart.AddDays(-cycle.LutealLength));
        DateTime pmsStart = NormalizeDate(nextPeriodStart.AddDays(-DefaultPmsWindow));

        return new CyclePredictionsModel(
            nextPeriodStart,
            ovulation,
            pmsStart);
    }

    private static DateTime NormalizeDate(DateTime date) =>
        DateTime.SpecifyKind(
            (date.Kind == DateTimeKind.Utc ? date : date.ToUniversalTime()).Date,
            DateTimeKind.Utc);
}
