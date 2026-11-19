using FoodDiary.Modules.Users.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Application.Commands.RecordUserActivity;
using FoodDiary.Modules.Users.Application.Queries.GetUserRegistrationTime;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Queries.GetUserRegistrationTime;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class UserActivityAndRegistrationTests {
    [Fact]
    public async Task Activity_RefusesUnavailableAccountsBeforeWritingTheActivity() {
        var id = UserId.New();
        var denied = new Error("Authentication.InvalidToken", "Unavailable account", ErrorKind.Unauthorized);
        ICurrentUserAccessService access = Substitute.For<ICurrentUserAccessService>();
        access.EnsureCanAccessAsync(id, CancellationToken.None).Returns(denied);
        IUserActivityRepository activity = Substitute.For<IUserActivityRepository>();

        Result result = await new RecordUserActivityCommandHandler(activity, access, TimeProvider.System)
            .Handle(new RecordUserActivityCommand(id.Value), CancellationToken.None);

        Assert.Equal(denied, ResultAssert.Failure(result));
        Assert.Empty(activity.ReceivedCalls());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RegistrationTime_PreservesTheLookupResultAndCancellationToken(bool exists) {
        var id = UserId.New();
        using var cancellation = new CancellationTokenSource();
        DateTime? registered = exists ? new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc) : null;
        IUserLookupRepository users = Substitute.For<IUserLookupRepository>();
        users.GetRegistrationTimeAsync(id, cancellation.Token).Returns(registered);

        DateTime? result = await new GetUserRegistrationTimeQueryHandler(users)
            .Handle(new GetUserRegistrationTimeQuery(id), cancellation.Token);

        Assert.Equal(registered, result);
        await users.Received(1).GetRegistrationTimeAsync(id, cancellation.Token);
        Assert.DoesNotContain(users.ReceivedCalls(), call => string.Equals(call.GetMethodInfo().Name, nameof(IUserLookupRepository.GetByIdAsync), StringComparison.Ordinal));
    }
}
