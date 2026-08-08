using FoodDiary.Application.Gamification.Models;
using FoodDiary.Application.Gamification.Queries.GetGamification;
using FoodDiary.Presentation.Api.Features.Gamification.Responses;

namespace FoodDiary.Presentation.Api.Features.Gamification.Mappings;

public static class GamificationHttpMappings {
    extension(Guid userId) {
        public GetGamificationQuery ToQuery() =>
                new(userId);
    }

    extension(GamificationModel model) {
        public GamificationHttpResponse ToHttpResponse() =>
                new(
                    model.CurrentStreak,
                    model.LongestStreak,
                    model.TotalMealsLogged,
                    model.HealthScore,
                    model.WeeklyAdherence,
                    model.Badges.Select(b => new BadgeHttpResponse(b.Key, b.Category, b.Threshold, b.IsEarned)).ToList());
    }
}
