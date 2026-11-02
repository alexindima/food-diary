using FoodDiary.Mediator;
using FoodDiary.Modules.Statistics.Application.Models;
using FoodDiary.Modules.Statistics.Application.Queries.GetDiaryStatistics;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Statistics.Application.Tests;

public sealed partial class DiaryStatisticsQueryTests {
    [Fact]
    public async Task DatedProfileFailure_DoesNotReportAnEmptyDiary() {
        var owner = UserId.New();
        IUserDashboardProfileReadService profiles = Substitute.For<IUserDashboardProfileReadService>();
        profiles.GetDashboardProfileAsync(owner, Arg.Any<CancellationToken>()).Returns(Result.Success(Profile(owner)));
        var failure = new Error("Profile.Unavailable", "Dated goals unavailable.");
        profiles.GetDashboardProfileAsync(owner, Arg.Any<CancellationToken>(),
            Arg.Is<DateOnly?>(date => date.HasValue), Arg.Is<DateOnly?>(date => date.HasValue))
            .Returns(Result.Failure<UserDashboardProfileModel>(failure));
        ISender sender = Substitute.For<ISender>();
        var handler = new GetDiaryStatisticsQueryHandler(Substitute.For<ICurrentUserAccessService>(), profiles, sender, new Clock());

        Result<DiaryStatisticsSummaryModel> result = await handler.Handle(new GetDiaryStatisticsQuery(owner.Value, 7), CancellationToken.None);

        Assert.Equal(failure, ResultAssert.Failure(result));
        Assert.Empty(sender.ReceivedCalls());
    }
}
