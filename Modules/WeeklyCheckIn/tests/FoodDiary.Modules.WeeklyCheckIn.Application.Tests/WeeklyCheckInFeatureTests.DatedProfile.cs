using FoodDiary.Mediator;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.WeeklyCheckIn.Application.Models;
using FoodDiary.Modules.WeeklyCheckIn.Application.Queries.GetWeeklyCheckIn;
using FoodDiary.Results;

namespace FoodDiary.Modules.WeeklyCheckIn.Application.Tests;

public sealed partial class WeeklyCheckInFeatureTests {
    [Fact]
    public async Task DatedProfileFailure_DoesNotReadTheCurrentOrPreviousWeek() {
        var user = User.Create("dated-week@example.test", "hash");
        IUserWeeklyCheckInProfileReadService profiles = CreateProfileService(user);
        var failure = new Error("Profile.Unavailable", "Dated goals unavailable.");
        profiles.GetWeeklyCheckInProfileAsync(user.Id, Arg.Any<CancellationToken>(),
            Arg.Is<DateOnly?>(date => date.HasValue), Arg.Is<DateOnly?>(date => date.HasValue))
            .Returns(Result.Failure<UserWeeklyCheckInProfileModel>(failure));
        ISender sender = Substitute.For<ISender>();
        GetWeeklyCheckInQueryHandler handler = CreateHandler(statisticsReadService: sender, profileService: profiles);

        Result<WeeklyCheckInModel> result = await handler.Handle(new GetWeeklyCheckInQuery(user.Id.Value), CancellationToken.None);

        Assert.Equal(failure, ResultAssert.Failure(result));
        Assert.Empty(sender.ReceivedCalls());
    }
}
