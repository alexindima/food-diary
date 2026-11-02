using FoodDiary.Modules.Users.Application.Commands.UpdatePrivacyConsent;
using FoodDiary.Modules.Users.Application.Common;
using FoodDiary.Modules.Users.Application.Queries.GetUserPrivacy;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.Privacy;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class PrivacyConsentHandlerTests {
    private static readonly DateTime Now = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Read_ReturnsTheAccessibleUsersPersistedPrivacyState() {
        var id = UserId.New();
        using var cancellation = new CancellationTokenSource();
        ICurrentUserAccessService access = Substitute.For<ICurrentUserAccessService>();
        IUserPrivacyStateReader reader = Substitute.For<IUserPrivacyStateReader>();
        var state = new UserPrivacyStateModel(PrivacyNoticeVersions.Current, DateTime.UtcNow, AiAcceptedAtUtc: null, MarketingAcceptedAtUtc: null);
        reader.GetAsync(id, cancellation.Token).Returns(state);

        Result<UserPrivacyStateModel> result = await new GetUserPrivacyQueryHandler(access, reader)
            .Handle(new GetUserPrivacyQuery(id.Value), cancellation.Token);

        Assert.Same(state, ResultAssert.Success(result));
        await access.Received(1).EnsureCanAccessAsync(id, cancellation.Token);
        await reader.Received(1).GetAsync(id, cancellation.Token);
    }

    [Fact]
    public async Task Read_WhenAccessIsDenied_DoesNotReadPrivateState() {
        var id = UserId.New();
        var denied = new Error("Privacy.Denied", "Denied", ErrorKind.Forbidden);
        ICurrentUserAccessService access = Substitute.For<ICurrentUserAccessService>();
        IUserPrivacyStateReader reader = Substitute.For<IUserPrivacyStateReader>();
        access.EnsureCanAccessAsync(id, Arg.Any<CancellationToken>()).Returns(denied);

        Result<UserPrivacyStateModel> result = await new GetUserPrivacyQueryHandler(access, reader)
            .Handle(new GetUserPrivacyQuery(id.Value), CancellationToken.None);

        Assert.Equal(denied, ResultAssert.Failure(result));
        await reader.DidNotReceiveWithAnyArgs().GetAsync(default, default);
    }

    [Fact]
    public async Task Read_WhenTheAccessibleUserDisappears_ReturnsNotFound() {
        var id = UserId.New();
        var handler = new GetUserPrivacyQueryHandler(Substitute.For<ICurrentUserAccessService>(), Substitute.For<IUserPrivacyStateReader>());

        Result<UserPrivacyStateModel> result = await handler.Handle(new GetUserPrivacyQuery(id.Value), CancellationToken.None);

        Assert.Equal(UserErrors.NotFound(id.Value), ResultAssert.Failure(result));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(2, false)]
    [InlineData(3, true)]
    [InlineData(3, false)]
    public async Task Update_RecordsTheDecisionAndPersistsTheAccessibleUser(int purpose, bool granted) {
        var user = User.Create("privacy@example.test", "hash");
        IUserContextService users = Substitute.For<IUserContextService>();
        users.GetAccessibleUserAsync(user.Id, Arg.Any<CancellationToken>()).Returns(Result.Success(user));
        using var cancellation = new CancellationTokenSource();
        var handler = new UpdatePrivacyConsentCommandHandler(users, new ConsentClock());

        Result result = await handler.Handle(new UpdatePrivacyConsentCommand(user.Id.Value, purpose, granted, PrivacyNoticeVersions.Current), cancellation.Token);

        ResultAssert.Success(result);
        UserPrivacyConsentEvent decision = Assert.Single(user.PrivacyConsentEvents);
        Assert.Multiple(
            () => Assert.Equal((PrivacyConsentPurpose)purpose, decision.Purpose),
            () => Assert.Equal(granted, decision.Granted),
            () => Assert.Equal(user.Id, decision.UserId),
            () => Assert.Equal(Now, decision.OccurredAtUtc),
            () => Assert.Equal(PrivacyNoticeVersions.Current, decision.NoticeVersion));
        DateTime? accepted = granted ? Now : null;
        switch ((PrivacyConsentPurpose)purpose) {
            case PrivacyConsentPurpose.HealthTracking:
                Assert.Multiple(
                    () => Assert.Equal(accepted, user.HealthConsentAcceptedAtUtc),
                    () => Assert.Equal(granted ? null : (DateTime?)Now, user.HealthConsentDeclinedAtUtc),
                    () => Assert.Equal(PrivacyNoticeVersions.Current, user.HealthConsentNoticeVersion));
                break;
            case PrivacyConsentPurpose.AiProcessing:
                Assert.Equal(accepted, user.AiConsentAcceptedAt);
                Assert.Equal(PrivacyNoticeVersions.Current, user.AiConsentNoticeVersion);
                break;
            case PrivacyConsentPurpose.MarketingAttribution:
                Assert.Equal(accepted, user.MarketingConsentAcceptedAtUtc);
                Assert.Equal(PrivacyNoticeVersions.Current, user.MarketingConsentNoticeVersion);
                break;
        }
        await users.Received(1).UpdateUserAsync(user, cancellation.Token);
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(1, "old")]
    [InlineData(1, " ")]
    public async Task Update_InvalidPurposeOrNotice_DoesNotLoadOrWriteUser(int purpose, string? notice) {
        IUserContextService users = Substitute.For<IUserContextService>();

        Result result = await new UpdatePrivacyConsentCommandHandler(users, TimeProvider.System)
            .Handle(new UpdatePrivacyConsentCommand(Guid.NewGuid(), purpose, Granted: true, notice ?? PrivacyNoticeVersions.Current), CancellationToken.None);

        Assert.Equal("Privacy.InvalidNotice", ResultAssert.Failure(result).Code);
        Assert.Empty(users.ReceivedCalls());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Update_AccessOrLoadFailure_DoesNotPersist(bool accessDenied) {
        var id = UserId.New();
        Error error = UserErrors.NotFound(id.Value);
        IUserContextService users = Substitute.For<IUserContextService>();
        if (accessDenied) {
            users.EnsureCanAccessAsync(id, Arg.Any<CancellationToken>()).Returns(error);
        } else {
            users.GetAccessibleUserAsync(id, Arg.Any<CancellationToken>()).Returns(Result.Failure<User>(error));
        }

        Result result = await new UpdatePrivacyConsentCommandHandler(users, TimeProvider.System)
            .Handle(new UpdatePrivacyConsentCommand(id.Value, 1, Granted: true, PrivacyNoticeVersions.Current), CancellationToken.None);

        Assert.Equal(error, ResultAssert.Failure(result));
        await users.DidNotReceiveWithAnyArgs().UpdateUserAsync(default!, default);
    }

    [ExcludeFromCodeCoverage]
    private sealed class ConsentClock : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(Now);
    }
}
