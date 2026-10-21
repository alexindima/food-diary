using FoodDiary.Mediator;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.WeeklyCheckIn.Application.Queries.GetWeeklyCheckIn;
using FoodDiary.Results;

namespace FoodDiary.Modules.WeeklyCheckIn.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class InvalidCalendarZoneTests {
    [Fact]
    public async Task UnknownZone_IsRejectedAfterAccessResolutionBeforeSummaryReadsOrClockAccess() {
        var user = UserId.New();
        using var cancellation = new CancellationTokenSource();
        ICurrentUserAccessService access = Substitute.For<ICurrentUserAccessService>();
        access.EnsureCanAccessAsync(user, cancellation.Token).Returns((Error?)null);
        IUserWeeklyCheckInProfileReadService profiles = Substitute.For<IUserWeeklyCheckInProfileReadService>();
        profiles.GetWeeklyCheckInProfileAsync(user, cancellation.Token).Returns(Result.Success(new UserWeeklyCheckInProfileModel(2000)));
        ISender summaries = Substitute.For<ISender>();
        TimeProvider clock = Substitute.For<TimeProvider>();
        var handler = new GetWeeklyCheckInQueryHandler(summaries, access, profiles, clock);

        Error error = ResultAssert.Failure(await handler.Handle(
            new GetWeeklyCheckInQuery(user.Value, TimeZoneId: "FoodDiary/Unknown"), cancellation.Token));

        Assert.Multiple(
            () => Assert.Equal("Validation.Invalid", error.Code),
            () => Assert.Contains("calendar time zone", error.Message, StringComparison.Ordinal),
            () => Assert.Empty(summaries.ReceivedCalls()),
            () => Assert.Empty(clock.ReceivedCalls()));
        await access.Received(1).EnsureCanAccessAsync(user, cancellation.Token);
        await profiles.Received(1).GetWeeklyCheckInProfileAsync(user, cancellation.Token);
    }
}
