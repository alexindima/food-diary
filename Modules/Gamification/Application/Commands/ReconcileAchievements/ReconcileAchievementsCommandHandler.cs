using FoodDiary.Mediator;
using FoodDiary.Modules.Gamification.Contracts.Commands.ReconcileAchievements;
using FoodDiary.Modules.Gamification.Application.Services;
using FoodDiary.Modules.Gamification.Application.Abstractions.Achievements.Common;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Modules.Gamification.Application.Common;
using FoodDiary.Modules.Gamification.Application.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Gamification.Application.Commands.ReconcileAchievements;

public sealed class ReconcileAchievementsCommandHandler(
    IMealActivityReadService mealActivityReadService,
    IAchievementMetricReader achievementMetricReader,
    IAchievementAwardService achievementAwardService,
    TimeProvider timeProvider) : IRequestHandler<ReconcileAchievementsCommand, Unit> {
    public async Task<Unit> Handle(ReconcileAchievementsCommand request, CancellationToken cancellationToken) {
        UserId userId = request.UserId;
        DateTime occurredAtUtc = request.OccurredAtUtc;
        DateTime today = timeProvider.GetUtcNow().UtcDateTime.Date;
        IReadOnlyList<DateTime> mealDates = await mealActivityReadService
            .GetDistinctMealDatesAsync(userId, DateTime.UnixEpoch, today, cancellationToken)
            .ConfigureAwait(false);
        (_, int longestStreak) = GamificationCalculator.CalculateStreaks(mealDates, today);
        int totalMeals = await mealActivityReadService.GetTotalMealCountAsync(userId, cancellationToken).ConfigureAwait(false);
        int totalAcademyArticlesRead = await achievementMetricReader
            .GetCompletedAcademyArticleCountAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        var metrics = new AchievementMetricSnapshot(longestStreak, totalMeals, totalAcademyArticlesRead);
        await achievementAwardService.EvaluateAndGrantAsync(userId, metrics, cancellationToken, occurredAtUtc).ConfigureAwait(false);
        return Unit.Value;
    }
}
