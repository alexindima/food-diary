using FoodDiary.Application.Gamification.Models;
using FoodDiary.Application.Gamification.Queries.GetGamification;
using FoodDiary.Presentation.Api.Features.Gamification.Responses;
using System.Globalization;

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
                    model.Badges.Select(b => new BadgeHttpResponse(
                        b.Key,
                        b.Category,
                        b.Threshold,
                        b.IsEarned,
                        IsRussianCulture() ? b.TitleRu : b.TitleEn,
                        IsRussianCulture() ? b.DescriptionRu : b.DescriptionEn,
                        b.Icon,
                        b.EarnedAtUtc)).ToList());
    }

    private static bool IsRussianCulture() =>
        string.Equals(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, "ru", StringComparison.OrdinalIgnoreCase);
}
