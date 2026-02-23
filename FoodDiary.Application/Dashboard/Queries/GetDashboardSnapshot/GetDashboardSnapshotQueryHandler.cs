using System.Text.Json;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Consumptions.Queries.GetConsumptions;
using FoodDiary.Application.DailyAdvices.Queries.GetDailyAdvice;
using FoodDiary.Application.Dashboard.Services;
using FoodDiary.Application.Hydration.Queries.GetHydrationDailyTotal;
using FoodDiary.Application.Statistics.Queries.GetStatistics;
using FoodDiary.Application.WaistEntries.Queries.GetWaistSummaries;
using FoodDiary.Application.WeightEntries.Queries.GetWeightSummaries;
using FoodDiary.Contracts.Dashboard;
using FoodDiary.Contracts.WaistEntries;
using FoodDiary.Contracts.WeightEntries;
using FoodDiary.Contracts.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using MediatR;

namespace FoodDiary.Application.Dashboard.Queries.GetDashboardSnapshot;

public class GetDashboardSnapshotQueryHandler(
    ISender sender,
    IUserRepository userRepository,
    IWeightEntryRepository weightEntryRepository,
    IWaistEntryRepository waistEntryRepository)
    : IQueryHandler<GetDashboardSnapshotQuery, Result<DashboardSnapshotResponse>> {
    public async Task<Result<DashboardSnapshotResponse>> Handle(GetDashboardSnapshotQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == UserId.Empty) {
            return Result.Failure<DashboardSnapshotResponse>(Errors.Authentication.InvalidToken);
        }

        var date = query.Date;
        var dayStart = NormalizeToUtcDate(date);
        var dayEnd = dayStart.AddDays(1).AddTicks(-1);
        var userId = query.UserId.Value;
        var locale = string.IsNullOrWhiteSpace(query.Locale) ? "en" : query.Locale;
        var trendDays = Math.Clamp(query.TrendDays <= 0 ? 7 : query.TrendDays, 1, 31);
        var trendStart = dayStart.AddDays(-(trendDays - 1));

        var statsTask = sender.Send(new GetStatisticsQuery(
            userId,
            dayStart,
            dayEnd,
            1), cancellationToken);
        var weeklyStatsTask = sender.Send(new GetStatisticsQuery(
            userId,
            dayStart.AddDays(-6),
            dayEnd,
            1), cancellationToken);
        var mealsTask = sender.Send(new GetConsumptionsQuery(
            userId,
            query.Page,
            query.PageSize,
            dayStart,
            dayEnd), cancellationToken);

        await Task.WhenAll(statsTask, weeklyStatsTask, mealsTask);
        var statsResult = await statsTask;
        if (statsResult.IsFailure) {
            return Result.Failure<DashboardSnapshotResponse>(statsResult.Error);
        }

        var weeklyStatsResult = await weeklyStatsTask;
        if (weeklyStatsResult.IsFailure) {
            return Result.Failure<DashboardSnapshotResponse>(weeklyStatsResult.Error);
        }

        var mealsResult = await mealsTask;
        if (mealsResult.IsFailure) {
            return Result.Failure<DashboardSnapshotResponse>(mealsResult.Error);
        }

        var userTask = userRepository.GetByIdAsync(userId);
        var weightEntriesTask = weightEntryRepository.GetEntriesAsync(
            userId,
            dateFrom: null,
            dateTo: null,
            limit: 2,
            descending: true,
            cancellationToken: cancellationToken);
        var waistEntriesTask = waistEntryRepository.GetEntriesAsync(
            userId,
            dateFrom: null,
            dateTo: null,
            limit: 2,
            descending: true,
            cancellationToken: cancellationToken);
        await Task.WhenAll(userTask, weightEntriesTask, waistEntriesTask);

        var user = await userTask;
        var weightEntries = await weightEntriesTask;
        var waistEntries = await waistEntriesTask;

        var statistics = DashboardMapping.ToStatisticsDto(statsResult.Value.FirstOrDefault(), user);
        var weeklyCalories = DashboardMapping.ToWeeklyCalories(weeklyStatsResult.Value);
        var weight = DashboardMapping.ToWeightDto(weightEntries, user?.DesiredWeight);
        var waist = DashboardMapping.ToWaistDto(waistEntries, user?.DesiredWaist);
        var meals = new DashboardMealsDto(
            mealsResult.Value.Data,
            mealsResult.Value.TotalItems);

        var hydrationTask = sender.Send(
            new GetHydrationDailyTotalQuery(userId, dayStart),
            cancellationToken);

        var adviceTask = sender.Send(
            new GetDailyAdviceQuery(userId, dayStart, locale),
            cancellationToken);

        var weightTrendTask = sender.Send(
            new GetWeightSummariesQuery(userId, trendStart, dayStart, 1),
            cancellationToken);

        var waistTrendTask = sender.Send(
            new GetWaistSummariesQuery(userId, trendStart, dayStart, 1),
            cancellationToken);
        await Task.WhenAll(hydrationTask, adviceTask, weightTrendTask, waistTrendTask);

        var hydrationResult = await hydrationTask;
        var adviceResult = await adviceTask;
        var weightTrendResult = await weightTrendTask;
        var waistTrendResult = await waistTrendTask;

        var dailyGoal = user?.DailyCalorieTarget ?? 0;
        DashboardLayoutSettings? layout = null;
        if (!string.IsNullOrWhiteSpace(user?.DashboardLayoutJson)) {
            try {
                layout = JsonSerializer.Deserialize<DashboardLayoutSettings>(user.DashboardLayoutJson!);
            } catch (JsonException) {
                layout = null;
            }
        }

        var response = new DashboardSnapshotResponse(
            date,
            dailyGoal,
            statistics,
            weeklyCalories,
            weight,
            waist,
            meals,
            hydrationResult.IsSuccess ? hydrationResult.Value : null,
            adviceResult.IsSuccess ? adviceResult.Value : null,
            weightTrendResult.IsSuccess ? weightTrendResult.Value : Array.Empty<WeightEntrySummaryResponse>(),
            waistTrendResult.IsSuccess ? waistTrendResult.Value : Array.Empty<WaistEntrySummaryResponse>(),
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
