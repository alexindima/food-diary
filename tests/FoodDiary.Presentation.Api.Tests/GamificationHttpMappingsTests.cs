using FoodDiary.Application.Gamification.Models;
using FoodDiary.Presentation.Api.Features.Gamification.Mappings;

namespace FoodDiary.Presentation.Api.Tests;

public sealed class GamificationHttpMappingsTests {
    [Fact]
    public void ToQuery_MapsUserId() {
        var userId = Guid.NewGuid();

        var query = userId.ToQuery();

        Assert.Equal(userId, query.UserId);
    }

    [Fact]
    public void GamificationModel_ToHttpResponse_MapsAllFields() {
        var badges = new List<BadgeModel> {
            new("streak_7", "Streaks", 7, true),
            new("meals_100", "Meals", 100, false),
        };
        var model = new GamificationModel(5, 12, 85, 72, 0.85, badges);

        var response = model.ToHttpResponse();

        Assert.Equal(5, response.CurrentStreak);
        Assert.Equal(12, response.LongestStreak);
        Assert.Equal(85, response.TotalMealsLogged);
        Assert.Equal(72, response.HealthScore);
        Assert.Equal(0.85, response.WeeklyAdherence);
        Assert.Equal(2, response.Badges.Count);
        Assert.Equal("streak_7", response.Badges[0].Key);
        Assert.True(response.Badges[0].IsEarned);
        Assert.False(response.Badges[1].IsEarned);
    }

    [Fact]
    public void GamificationModel_ToHttpResponse_WithEmptyBadges() {
        var model = new GamificationModel(0, 0, 0, 0, 0, []);

        var response = model.ToHttpResponse();

        Assert.Empty(response.Badges);
    }
}
