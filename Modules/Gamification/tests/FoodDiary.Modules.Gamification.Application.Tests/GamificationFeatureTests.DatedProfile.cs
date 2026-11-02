using FoodDiary.Modules.Gamification.Application.Models;
using FoodDiary.Modules.Gamification.Application.Queries.GetGamification;
using FoodDiary.Modules.Meals.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Results;

namespace FoodDiary.Modules.Gamification.Application.Tests;

public partial class GamificationFeatureTests {
    [Fact]
    public async Task DatedProfileFailure_DoesNotReadNutrition() {
        var user = User.Create("dated-profile@example.test", "hash");
        IUserGamificationProfileReadService profiles = CreateUserProfileService(user);
        var failure = new Error("Profile.Unavailable", "Dated goals unavailable.");
        profiles.GetGamificationProfileAsync(user.Id, Arg.Any<CancellationToken>(),
            Arg.Is<DateOnly?>(date => date.HasValue), Arg.Is<DateOnly?>(date => date.HasValue))
            .Returns(Result.Failure<UserGamificationProfileModel>(failure));
        IMealNutritionStatisticsReadService statistics = CreateStatisticsReadService();
        GetGamificationQueryHandler handler = CreateGamificationHandler(CreateMealRepository(), statistics, profiles);

        Result<GamificationModel> result = await handler.Handle(new GetGamificationQuery(user.Id.Value), CancellationToken.None);

        Assert.Equal(failure, ResultAssert.Failure(result));
        Assert.Empty(statistics.ReceivedCalls());
    }
}
