using System.Text.Json;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Consumptions.Queries.GetConsumptions;
using FoodDiary.Application.DailyAdvices.Queries.GetDailyAdvice;
using FoodDiary.Application.Dashboard.Models;
using FoodDiary.Application.Dashboard.Services;
using FoodDiary.Application.Hydration.Queries.GetHydrationDailyTotal;
using FoodDiary.Application.Statistics.Queries.GetStatistics;
using FoodDiary.Application.Users.Models;
using FoodDiary.Application.WaistEntries.Queries.GetWaistSummaries;
using FoodDiary.Application.WeightEntries.Queries.GetWeightSummaries;
using FoodDiary.Domain.ValueObjects.Ids;
using MediatR;

namespace FoodDiary.Application.Dashboard.Queries.GetDashboardSnapshot;

public class GetDashboardSnapshotQueryHandler(
    ISender sender,
    IUserRepository userRepository,
    IWeightEntryRepository weightEntryRepository,
    IWaistEntryRepository waistEntryRepository)
    : IQueryHandler<GetDashboardSnapshotQuery, Result<DashboardSnapshotModel>> {
    public async Task<Result<DashboardSnapshotModel>> Handle(GetDashboardSnapshotQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == UserId.Empty) {
            return Result.Failure<DashboardSnapshotModel>(Errors.Authentication.InvalidToken);
        }

        var date = query.Date;
        var dayStart = NormalizeToUtcDate(date);
        var dayEnd = dayStart.AddDays(1).AddTicks(-1);
        var userId = query.UserId.Value;
        var locale = string.IsNullOrWhiteSpace(query.Locale) ? "en" : query.Locale;
        var trendDays = Math.Clamp(query.TrendDays <= 0 ? 7 : query.TrendDays, 1, 31);
        var trendStart = dayStart.AddDays(-(trendDays - 1));

        var statsResult = await sender.Send(new GetStatisticsQuery(
            userId,
            dayStart,
            dayEnd,
            1), cancellationToken);
        if (statsResult.IsFailure) {
            return Result.Failure<DashboardSnapshotModel>(statsResult.Error);
        }

        var weeklyStatsResult = await sender.Send(new GetStatisticsQuery(
            userId,
            dayStart.AddDays(-6),
            dayEnd,
            1), cancellationToken);
        if (weeklyStatsResult.IsFailure) {
            return Result.Failure<DashboardSnapshotModel>(weeklyStatsResult.Error);
        }

        var mealsResult = await sender.Send(new GetConsumptionsQuery(
            userId,
            query.Page,
            query.PageSize,
            dayStart,
            dayEnd), cancellationToken);
        if (mealsResult.IsFailure) {
            return Result.Failure<DashboardSnapshotModel>(mealsResult.Error);
        }

        var user = await userRepository.GetByIdAsync(userId);
        var weightEntries = await weightEntryRepository.GetEntriesAsync(
            userId,
            dateFrom: null,
            dateTo: null,
            limit: 2,
            descending: true,
            cancellationToken: cancellationToken);
        var waistEntries = await waistEntryRepository.GetEntriesAsync(
            userId,
            dateFrom: null,
            dateTo: null,
            limit: 2,
            descending: true,
            cancellationToken: cancellationToken);

        var statistics = DashboardMapping.ToStatisticsModel(statsResult.Value.FirstOrDefault(), user);
        var weeklyCalories = DashboardMapping.ToWeeklyCalories(weeklyStatsResult.Value);
        var weight = DashboardMapping.ToWeightModel(weightEntries, user?.DesiredWeight);
        var waist = DashboardMapping.ToWaistModel(waistEntries, user?.DesiredWaist);
        var meals = new DashboardMealsModel(
            mealsResult.Value.Data,
            mealsResult.Value.TotalItems);

        var hydrationResult = await sender.Send(
            new GetHydrationDailyTotalQuery(userId, dayStart),
            cancellationToken);

        var adviceResult = await sender.Send(
            new GetDailyAdviceQuery(userId, dayStart, locale),
            cancellationToken);

        var weightTrendResult = await sender.Send(
            new GetWeightSummariesQuery(userId, trendStart, dayStart, 1),
            cancellationToken);

        var waistTrendResult = await sender.Send(
            new GetWaistSummariesQuery(userId, trendStart, dayStart, 1),
            cancellationToken);

        var dailyGoal = user?.DailyCalorieTarget ?? 0;
        DashboardLayoutModel? layout = null;
        if (!string.IsNullOrWhiteSpace(user?.DashboardLayoutJson)) {
            try {
                layout = JsonSerializer.Deserialize<DashboardLayoutModel>(user.DashboardLayoutJson!);
            } catch (JsonException) {
                layout = null;
            }
        }

        var response = new DashboardSnapshotModel(
            date,
            dailyGoal,
            statistics,
            weeklyCalories,
            weight,
            waist,
            meals,
            hydrationResult.IsSuccess ? hydrationResult.Value : null,
            adviceResult.IsSuccess ? adviceResult.Value : null,
            weightTrendResult.IsSuccess ? weightTrendResult.Value : Array.Empty<FoodDiary.Application.WeightEntries.Models.WeightEntrySummaryModel>(),
            waistTrendResult.IsSuccess ? waistTrendResult.Value : Array.Empty<FoodDiary.Application.WaistEntries.Models.WaistEntrySummaryModel>(),
            layout);

        return Result.Success(response);
    }

    private static DateTime NormalizeToUtcDate(DateTime value) {
        var utc = value.Kind switch {
            DateTimeKind.Utc => value,
            _ => value.ToUniversalTime()
        };

        return DateTime.SpecifyKind(utc.Date, DateTimeKind.Utc);
    }
}
