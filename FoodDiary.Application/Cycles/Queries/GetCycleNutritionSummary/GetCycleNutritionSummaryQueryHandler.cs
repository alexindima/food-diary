using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Cycles.Common;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Cycles.Queries.GetCycleNutritionSummary;

public class GetCycleNutritionSummaryQueryHandler(
    ICycleRepository cycleRepository,
    IMealRepository mealRepository,
    IUserRepository userRepository)
    : IQueryHandler<GetCycleNutritionSummaryQuery, Result<CycleNutritionSummaryModel?>> {
    private const int MaxSummaryRangeDays = 366;

    public async Task<Result<CycleNutritionSummaryModel?>> Handle(
        GetCycleNutritionSummaryQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<CycleNutritionSummaryModel?>(userIdResult.Error);
        }

        DateTime normalizedFrom = UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(query.DateFrom);
        DateTime normalizedTo = UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(query.DateTo);
        if (normalizedFrom > normalizedTo) {
            return Result.Failure<CycleNutritionSummaryModel?>(
                Errors.Validation.Invalid(nameof(query.DateFrom), "DateFrom must be less than or equal to DateTo."));
        }

        if ((normalizedTo - normalizedFrom).TotalDays > MaxSummaryRangeDays) {
            return Result.Failure<CycleNutritionSummaryModel?>(
                Errors.Validation.Invalid(nameof(query.DateTo), "Summary range must not exceed one year."));
        }

        UserId userId = userIdResult.Value;
        Error? accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<CycleNutritionSummaryModel?>(accessError);
        }

        CycleProfile? profile = await cycleRepository.GetCurrentAsync(
            userId,
            includeDetails: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (profile is null) {
            return Result.Success<CycleNutritionSummaryModel?>(value: null);
        }

        IReadOnlyList<Meal> meals = await mealRepository.GetByPeriodAsync(
            userId,
            normalizedFrom.Date,
            normalizedTo.Date.AddDays(1).AddTicks(-1),
            cancellationToken).ConfigureAwait(false);

        return Result.Success<CycleNutritionSummaryModel?>(BuildSummary(profile, meals, normalizedFrom, normalizedTo));
    }

    private static CycleNutritionSummaryModel BuildSummary(
        CycleProfile profile,
        IReadOnlyCollection<Meal> meals,
        DateTime dateFrom,
        DateTime dateTo) {
        var mealsByDate = meals
            .GroupBy(meal => meal.Date.Date)
            .ToDictionary(group => group.Key, group => group.ToList());
        List<CycleNutritionDay> days = BuildCycleDays(profile, mealsByDate, dateFrom.Date, dateTo.Date);
        var bleedingDays = days.Where(day => day.IsBleeding && day.HasMeals).ToList();
        var nonBleedingDays = days.Where(day => !day.IsBleeding && day.HasMeals).ToList();

        return new CycleNutritionSummaryModel(
            dateFrom,
            dateTo,
            days.Count,
            days.Count(day => day.HasMeals),
            days.Count(day => day.IsBleeding),
            Average(bleedingDays, day => day.Calories),
            Average(nonBleedingDays, day => day.Calories),
            Average(bleedingDays, day => day.Fiber),
            Average(nonBleedingDays, day => day.Fiber),
            Average(days.Where(day => day.HasMeals && day.PainImpact.HasValue), day => day.PainImpact ?? 0));
    }

    private static List<CycleNutritionDay> BuildCycleDays(
        CycleProfile profile,
        IReadOnlyDictionary<DateTime, List<Meal>> mealsByDate,
        DateTime dateFrom,
        DateTime dateTo) {
        DateTime[] logDates = [
            .. profile.BleedingEntries.Select(entry => entry.Date.Date),
            .. profile.SymptomEntries.Select(entry => entry.Date.Date),
            .. profile.FertilitySignals.Select(signal => signal.Date.Date),
        ];

        return [
            .. logDates
            .Where(date => date >= dateFrom && date <= dateTo)
            .Distinct()
            .Order()
            .Select(date => BuildDay(profile, mealsByDate, date)),
        ];
    }

    private static CycleNutritionDay BuildDay(
        CycleProfile profile,
        IReadOnlyDictionary<DateTime, List<Meal>> mealsByDate,
        DateTime date) {
        mealsByDate.TryGetValue(date, out List<Meal>? meals);
        IReadOnlyCollection<Meal> dayMeals = meals ?? [];
        IReadOnlyCollection<BleedingEntry> bleedingEntries = [
            .. profile.BleedingEntries
            .Where(entry => entry.Date.Date == date),
        ];

        return new CycleNutritionDay(
            date,
            dayMeals.Count > 0,
            bleedingEntries.Any(entry => entry.Type == BleedingType.Bleeding),
            dayMeals.Sum(meal => meal.TotalCalories),
            dayMeals.Sum(meal => meal.TotalFiber),
            bleedingEntries.Select(entry => entry.PainImpact).FirstOrDefault(value => value.HasValue));
    }

    private static double Average(IEnumerable<CycleNutritionDay> days, Func<CycleNutritionDay, double> selector) {
        var items = days.ToList();
        return items.Count == 0
            ? 0
            : Math.Round(items.Average(selector), 2, MidpointRounding.ToEven);
    }

    private sealed record CycleNutritionDay(
        DateTime Date,
        bool HasMeals,
        bool IsBleeding,
        double Calories,
        double Fiber,
        int? PainImpact);
}
