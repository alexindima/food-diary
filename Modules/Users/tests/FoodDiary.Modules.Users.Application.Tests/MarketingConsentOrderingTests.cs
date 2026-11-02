using FoodDiary.Modules.Users.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Application.Common;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.Privacy;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class MarketingConsentOrderingTests {
    [Fact]
    public async Task DelayedSignupOptIn_DoesNotUndoAnExplicitWithdrawal() {
        var user = User.Create("consent@example.test", "hash");
        user.RecordPrivacyConsent(PrivacyConsentPurpose.MarketingAttribution, granted: false, PrivacyNoticeVersions.Current, DateTime.UtcNow);
        IUserLookupRepository lookup = Substitute.For<IUserLookupRepository>();
        lookup.GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(user);
        var service = new UserContextService(lookup, Substitute.For<IUserWriteRepository>());

        Result result = await service.RecordOptInAsync(user.Id, PrivacyNoticeVersions.Current, DateTime.UtcNow, CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Null(user.MarketingConsentAcceptedAtUtc);
        Assert.Single(user.PrivacyConsentEvents);
    }
}
