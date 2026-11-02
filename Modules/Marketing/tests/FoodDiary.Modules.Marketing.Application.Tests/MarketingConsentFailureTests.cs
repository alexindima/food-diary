using FoodDiary.Modules.Marketing.Application.Abstractions.Common;
using FoodDiary.Modules.Marketing.Application.Commands.RecordMarketingAttribution;
using FoodDiary.Modules.Marketing.Application.Commands.RecordPremiumConversion;
using FoodDiary.Modules.Marketing.Contracts.Commands.RecordPremiumConversion;
using FoodDiary.Modules.Marketing.Domain.ValueObjects;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.Privacy;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Marketing.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class MarketingConsentFailureTests {
    [Fact]
    public async Task SignupConsentFailure_PropagatesToTheCallerOwnedTransaction() {
        var owner = UserId.New();
        DateTime now = DateTime.UtcNow;
        IMarketingAttributionEventRepository repository = Substitute.For<IMarketingAttributionEventRepository>();
        var landing = new MarketingAttributionEventRecord("page_landing", now, UserId: null,
            new AnonymousVisitorId("owned-visitor"), new MarketingSessionId("owned-session"), "/", ReferrerHost: null,
            UtmSource: null, UtmMedium: null, UtmCampaign: null, UtmContent: null, UtmTerm: null, BuildVersion: null);
        repository.GetLandingAsync(landing.AnonymousId, landing.SessionId, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(landing);
        IUserMarketingConsentRecorder recorder = Substitute.For<IUserMarketingConsentRecorder>();
        var failure = new Error("Privacy.MarketingConsentRequired", "Withdrawn.", ErrorKind.Forbidden);
        recorder.RecordOptInAsync(owner, PrivacyNoticeVersions.Current, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(Result.Failure(failure));
        var command = new RecordMarketingAttributionCommand("signup_completed", now.ToString("O"), owner,
            landing.AnonymousId, landing.SessionId, "/", ReferrerHost: null, UtmSource: null, UtmMedium: null,
            UtmCampaign: null, UtmContent: null, UtmTerm: null, BuildVersion: null,
            Guid.NewGuid(), PrivacyNoticeVersions.Current, now.AddMinutes(-1));

        Result result = await new RecordMarketingAttributionCommandHandler(repository, TimeProvider.System, recorder)
            .Handle(command, CancellationToken.None);

        Assert.Equal(failure, ResultAssert.Failure(result));
        await recorder.Received(1).RecordOptInAsync(owner, PrivacyNoticeVersions.Current, Arg.Any<DateTime>(), CancellationToken.None);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PremiumConversion_WithoutPersistedMarketingOptInDoesNotReadOrWriteAttribution(bool missingProfile) {
        var owner = UserId.New();
        IUserPrivacyStateReader privacy = Substitute.For<IUserPrivacyStateReader>();
        privacy.GetAsync(owner, Arg.Any<CancellationToken>()).Returns(missingProfile ? null : new UserPrivacyStateModel(
            PrivacyNoticeVersions.Current, DateTime.UtcNow, DateTime.UtcNow, MarketingAcceptedAtUtc: null));
        IMarketingAttributionEventRepository repository = Substitute.For<IMarketingAttributionEventRepository>();

        await new RecordPremiumConversionCommandHandler(repository, repository, TimeProvider.System, privacy)
            .Handle(new RecordPremiumConversionCommand(owner.Value), CancellationToken.None);

        Assert.Empty(repository.ReceivedCalls());
    }
}
