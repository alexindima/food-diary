using System.Collections.Generic;
using System.Linq;
using FoodDiary.Contracts.Dashboard;
using FoodDiary.Contracts.Statistics;
using FoodDiary.Domain.Entities;

namespace FoodDiary.Application.Dashboard.Services;

public static class DashboardMapping
{
    public static DashboardStatisticsDto ToStatisticsDto(AggregatedStatisticsResponse? response, User? user)
    {
        if (response is null)
        {
            return new DashboardStatisticsDto(0, 0, 0, 0, 0, null, null, null, null);
        }

        return new DashboardStatisticsDto(
            response.TotalCalories,
            response.AverageProteins,
            response.AverageFats,
            response.AverageCarbs,
            response.AverageFiber,
            user?.ProteinTarget,
            user?.FatTarget,
            user?.CarbTarget,
            null);
    }

    public static DashboardWeightDto ToWeightDto(IReadOnlyList<WeightEntry> entries, double? desired)
    {
        var latest = entries.FirstOrDefault();
        var previous = entries.Skip(1).FirstOrDefault();

        return new DashboardWeightDto(
            latest is null ? null : new WeightEntryDto(latest.Date, latest.Weight),
            previous is null ? null : new WeightEntryDto(previous.Date, previous.Weight),
            desired);
    }

    public static DashboardWaistDto ToWaistDto(IReadOnlyList<WaistEntry> entries, double? desired)
    {
        var latest = entries.FirstOrDefault();
        var previous = entries.Skip(1).FirstOrDefault();

        return new DashboardWaistDto(
            latest is null ? null : new WaistEntryDto(latest.Date, latest.Circumference),
            previous is null ? null : new WaistEntryDto(previous.Date, previous.Circumference),
            desired);
    }
}
