using FoodDiary.Modules.Users.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Application.Common;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.Privacy;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class MarketingOptInBoundaryTests {
    [Fact]
    public async Task FirstOptIn_PersistsOneDatedEventAndRepeatedOptInPreservesTheOriginalChoice() {
        var user = User.Create("first-opt-in@example.test", "hash");
        IUserLookupRepository lookup = Substitute.For<IUserLookupRepository>();
        lookup.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        IUserWriteRepository writer = Substitute.For<IUserWriteRepository>();
        var service = new UserContextService(lookup, writer);
        DateTime now = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);
        using var cancellation = new CancellationTokenSource();

        ResultAssert.Success(await service.RecordOptInAsync(user.Id, PrivacyNoticeVersions.Current, now, cancellation.Token));
        ResultAssert.Success(await service.RecordOptInAsync(user.Id, PrivacyNoticeVersions.Current, now.AddMinutes(1), cancellation.Token));

        UserPrivacyConsentEvent consent = Assert.Single(user.PrivacyConsentEvents);
        Assert.Multiple(
            () => Assert.Equal(PrivacyConsentPurpose.MarketingAttribution, consent.Purpose),
            () => Assert.True(consent.Granted),
            () => Assert.Equal(now, user.MarketingConsentAcceptedAtUtc),
            () => Assert.Equal(PrivacyNoticeVersions.Current, user.MarketingConsentNoticeVersion));
        await writer.Received(1).UpdateAsync(user, cancellation.Token);
    }

    [Theory]
    [InlineData(true, "Privacy.InvalidNotice")]
    [InlineData(false, "Authentication.InvalidToken")]
    public async Task InvalidNoticeOrMissingAccount_DoesNotPersistAChoice(bool invalidNotice, string expected) {
        IUserLookupRepository lookup = Substitute.For<IUserLookupRepository>();
        IUserWriteRepository writer = Substitute.For<IUserWriteRepository>();
        var service = new UserContextService(lookup, writer);

        Result result = await service.RecordOptInAsync(UserId.New(), invalidNotice ? "obsolete" : PrivacyNoticeVersions.Current,
            DateTime.UtcNow, CancellationToken.None);

        ResultAssert.Failure(result, expected);
        Assert.Empty(writer.ReceivedCalls());
        if (invalidNotice) {
            Assert.Empty(lookup.ReceivedCalls());
        }
    }
}
