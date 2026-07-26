using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Dietologist;

internal sealed class AttentionSignalMetricsReadService(FoodDiaryDbContext context)
    : IAttentionSignalMetricsReadService {
    public async Task<IReadOnlyList<AttentionSignalMetricsReadModel>> GetAsync(
        IReadOnlyCollection<UserId> clientUserIds,
        DateTime dateFromUtc,
        DateTime dateToUtc,
        CancellationToken cancellationToken = default) {
        if (clientUserIds.Count == 0) {
            return [];
        }

        UserId[] ids = [.. clientUserIds.Distinct()];
        List<UserGoalProjection> goals = await context.Users
            .AsNoTracking()
            .Where(user => ids.Contains(user.Id))
            .Select(user => new UserGoalProjection(user.Id, user.DailyCalorieTarget ?? 0))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        List<MealProjection> meals = await context.Meals
            .AsNoTracking()
            .Where(meal =>
                ids.Contains(meal.UserId) &&
                meal.Date >= dateFromUtc &&
                meal.Date <= dateToUtc)
            .Select(meal => new MealProjection(
                meal.UserId,
                meal.Date,
                meal.ManualCalories ?? meal.TotalCalories))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        List<LastMealProjection> lastMeals = await context.Meals
            .AsNoTracking()
            .Where(meal => ids.Contains(meal.UserId))
            .GroupBy(meal => meal.UserId)
            .Select(group => new LastMealProjection(group.Key, group.Max(meal => meal.Date)))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        List<WeightProjection> weights = await context.WeightEntries
            .AsNoTracking()
            .Where(entry =>
                ids.Contains(entry.UserId) &&
                entry.Date >= dateFromUtc &&
                entry.Date <= dateToUtc)
            .OrderBy(entry => entry.Date)
            .Select(entry => new WeightProjection(entry.UserId, entry.Date, entry.Weight))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return BuildReadModels(goals, meals, lastMeals, weights);
    }

    private static IReadOnlyList<AttentionSignalMetricsReadModel> BuildReadModels(
        IReadOnlyList<UserGoalProjection> goals,
        IReadOnlyList<MealProjection> meals,
        IReadOnlyList<LastMealProjection> lastMeals,
        IReadOnlyList<WeightProjection> weights) {
        IReadOnlyDictionary<UserId, DateTime> lastMealByUser = lastMeals.ToDictionary(item => item.UserId, item => item.Date);
        ILookup<UserId, MealProjection> mealsByUser = meals.ToLookup(item => item.UserId);
        ILookup<UserId, WeightProjection> weightsByUser = weights.ToLookup(item => item.UserId);

        return [
            .. goals.Select(goal => new AttentionSignalMetricsReadModel(
                goal.UserId.Value,
                goal.DailyCalorieTarget,
                lastMealByUser.GetValueOrDefault(goal.UserId),
                [
                    .. mealsByUser[goal.UserId]
                        .GroupBy(meal => meal.Date.Date)
                        .OrderBy(group => group.Key)
                        .Select(group => new AttentionSignalDailyCaloriesReadModel(
                            DateTime.SpecifyKind(group.Key, DateTimeKind.Utc),
                            group.Sum(meal => meal.Calories))),
                ],
                [
                    .. weightsByUser[goal.UserId]
                        .Select(entry => new AttentionSignalWeightPointReadModel(entry.Date, entry.Weight)),
                ])),
        ];
    }

    private sealed record UserGoalProjection(UserId UserId, double DailyCalorieTarget);
    private sealed record MealProjection(UserId UserId, DateTime Date, double Calories);
    private sealed record LastMealProjection(UserId UserId, DateTime Date);
    private sealed record WeightProjection(UserId UserId, DateTime Date, double Weight);
}
