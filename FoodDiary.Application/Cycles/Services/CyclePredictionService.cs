using System;
using FoodDiary.Contracts.Cycles;
using FoodDiary.Domain.Entities;

namespace FoodDiary.Application.Cycles.Services;

public static class CyclePredictionService
{
    private const int DefaultPmsWindow = 5;

    public static CyclePredictionsResponse CalculatePredictions(Cycle cycle)
    {
        var nextPeriodStart = NormalizeDate(cycle.StartDate.AddDays(cycle.AverageLength));
        var ovulation = NormalizeDate(nextPeriodStart.AddDays(-cycle.LutealLength));
        var pmsStart = NormalizeDate(nextPeriodStart.AddDays(-DefaultPmsWindow));

        return new CyclePredictionsResponse(
            nextPeriodStart,
            ovulation,
            pmsStart);
    }

    private static DateTime NormalizeDate(DateTime date) =>
        date.Kind == DateTimeKind.Utc
            ? date.Date
            : DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
}
