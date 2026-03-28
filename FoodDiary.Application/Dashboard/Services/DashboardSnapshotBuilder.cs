using System.Text.Json;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.WaistEntries.Common;
using FoodDiary.Application.WeightEntries.Common;
using FoodDiary.Application.Consumptions.Queries.GetConsumptions;
using FoodDiary.Application.DailyAdvices.Queries.GetDailyAdvice;
using FoodDiary.Application.Dashboard.Models;
using FoodDiary.Application.Users.Models;
using FoodDiary.Application.Hydration.Queries.GetHydrationDailyTotal;
using FoodDiary.Application.Statistics.Queries.GetStatistics;
using FoodDiary.Application.WaistEntries.Queries.GetWaistSummaries;
using FoodDiary.Application.WeightEntries.Queries.GetWeightSummaries;
using FoodDiary.Domain.ValueObjects.Ids;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Application.Dashboard.Services;

public interface IDashboardSnapshotBuilder {
    Task<Result<DashboardSnapshotModel>> BuildAsync(
        DashboardSnapshotRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record DashboardSnapshotRequest(
    Guid UserId,
    DateTime Date,
    string Locale,
    int TrendDays,
    int Page,
    int PageSize);

public class DashboardSnapshotBuilder(
    ISender sender,
    IUserRepository userRepository,
    IWeightEntryRepository weightEntryRepository,
    IWaistEntryRepository waistEntryRepository,
    ILogger<DashboardSnapshotBuilder> logger) : IDashboardSnapshotBuilder {

    public async Task<Result<DashboardSnapshotModel>> BuildAsync(
        DashboardSnapshotRequest request,
        CancellationToken cancellationToken = default) {
        var dayStart = NormalizeToUtcDate(request.Date);
        var dayEnd = dayStart.AddDays(1).AddTicks(-1);
        var userId = new UserId(request.UserId);
        var locale = string.IsNullOrWhiteSpace(request.Locale) ? "en" : request.Locale;
        var trendDays = Math.Clamp(request.TrendDays <= 0 ? 7 : request.TrendDays, 1, 31);
        var trendStart = dayStart.AddDays(-(trendDays - 1));

        var statsResult = await sender.Send(new GetStatisticsQuery(
            request.UserId, dayStart, dayEnd, 1), cancellationToken);
        if (statsResult.IsFailure) return Result.Failure<DashboardSnapshotModel>(statsResult.Error);

        var weeklyStatsResult = await sender.Send(new GetStatisticsQuery(
            request.UserId, dayStart.AddDays(-6), dayEnd, 1), cancellationToken);
        if (weeklyStatsResult.IsFailure) return Result.Failure<DashboardSnapshotModel>(weeklyStatsResult.Error);

        var mealsResult = await sender.Send(new GetConsumptionsQuery(
            request.UserId, request.Page, request.PageSize, dayStart, dayEnd), cancellationToken);
        if (mealsResult.IsFailure) return Result.Failure<DashboardSnapshotModel>(mealsResult.Error);

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        var weightEntries = await weightEntryRepository.GetEntriesAsync(
            userId, dateFrom: null, dateTo: null, limit: 2, descending: true, cancellationToken: cancellationToken);
        var waistEntries = await waistEntryRepository.GetEntriesAsync(
            userId, dateFrom: null, dateTo: null, limit: 2, descending: true, cancellationToken: cancellationToken);

        var statistics = DashboardMapping.ToStatisticsModel(statsResult.Value.FirstOrDefault(), user);
        var weeklyCalories = DashboardMapping.ToWeeklyCalories(weeklyStatsResult.Value);
        var weight = DashboardMapping.ToWeightModel(weightEntries, user?.DesiredWeight);
        var waist = DashboardMapping.ToWaistModel(waistEntries, user?.DesiredWaist);
        var meals = new DashboardMealsModel(mealsResult.Value.Data, mealsResult.Value.TotalItems);

        var hydrationResult = await sender.Send(
            new GetHydrationDailyTotalQuery(request.UserId, dayStart), cancellationToken);

        var adviceResult = await sender.Send(
            new GetDailyAdviceQuery(userId, dayStart, locale), cancellationToken);

        var weightTrendResult = await sender.Send(
            new GetWeightSummariesQuery(userId, trendStart, dayStart, 1), cancellationToken);

        var waistTrendResult = await sender.Send(
            new GetWaistSummariesQuery(userId, trendStart, dayStart, 1), cancellationToken);

        var layout = ParseDashboardLayout(user?.DashboardLayoutJson, userId);

        return Result.Success(new DashboardSnapshotModel(
            request.Date,
            user?.DailyCalorieTarget ?? 0,
            statistics,
            weeklyCalories,
            weight,
            waist,
            meals,
            hydrationResult.IsSuccess ? hydrationResult.Value : null,
            adviceResult.IsSuccess ? adviceResult.Value : null,
            weightTrendResult.IsSuccess ? weightTrendResult.Value : [],
            waistTrendResult.IsSuccess ? waistTrendResult.Value : [],
            layout));
    }

    private DashboardLayoutModel? ParseDashboardLayout(string? json, UserId userId) {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try {
            return JsonSerializer.Deserialize<DashboardLayoutModel>(json);
        } catch (JsonException ex) {
            logger.LogWarning(ex, "Failed to deserialize dashboard layout JSON for user {UserId}", userId);
            return null;
        }
    }

    private static DateTime NormalizeToUtcDate(DateTime value) {
        var utc = value.Kind switch {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
        return DateTime.SpecifyKind(utc.Date, DateTimeKind.Utc);
    }
}
