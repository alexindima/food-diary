using FoodDiary.Contracts.Cycles;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.Cycles.Services;

public static class CyclePredictionService {
    private const int DefaultPmsWindow = 5;

    public static CyclePredictionsResponse CalculatePredictions(Cycle cycle) {
        ArgumentNullException.ThrowIfNull(cycle);

        var nextPeriodStart = NormalizeDate(cycle.StartDate.AddDays(cycle.AverageLength));
        var ovulation = NormalizeDate(nextPeriodStart.AddDays(-cycle.LutealLength));
        var pmsStart = NormalizeDate(nextPeriodStart.AddDays(-DefaultPmsWindow));

        return new CyclePredictionsResponse(
            nextPeriodStart,
            ovulation,
            pmsStart);
    }

    private static DateTime NormalizeDate(DateTime date) =>
        DateTime.SpecifyKind(
            (date.Kind == DateTimeKind.Utc ? date : date.ToUniversalTime()).Date,
            DateTimeKind.Utc);
}
