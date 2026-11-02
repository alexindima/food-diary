using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.Privacy;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Users.Infrastructure.IntegrationTests.Integration;

[ExcludeFromCodeCoverage]
public sealed class UserPrivacyConsentTrackingTests {
    [Theory]
    [InlineData(PrivacyConsentPurpose.HealthTracking)]
    [InlineData(PrivacyConsentPurpose.AiProcessing)]
    [InlineData(PrivacyConsentPurpose.MarketingAttribution)]
    public void Grant_OnTrackedAccount_InsertsNewConsentEvent(PrivacyConsentPurpose purpose) {
        using UsersDbContext context = CreateContext();
        var user = User.Create("consent-tracking@example.com", "hash");
        context.Users.Attach(user);

        user.RecordPrivacyConsent(purpose, granted: true, PrivacyNoticeVersions.Current, DateTime.UtcNow);
        context.ChangeTracker.DetectChanges();

        UserPrivacyConsentEvent consent = Assert.Single(user.PrivacyConsentEvents);
        Assert.Equal(EntityState.Added, context.Entry(consent).State);
    }

    [Theory]
    [InlineData(PrivacyConsentPurpose.HealthTracking)]
    [InlineData(PrivacyConsentPurpose.AiProcessing)]
    [InlineData(PrivacyConsentPurpose.MarketingAttribution)]
    public void Revoke_OnTrackedAccount_InsertsEventWithoutUpdatingPriorConsent(PrivacyConsentPurpose purpose) {
        using UsersDbContext context = CreateContext();
        var user = User.Create("consent-history@example.com", "hash");
        user.RecordPrivacyConsent(purpose, granted: true, PrivacyNoticeVersions.Current, DateTime.UtcNow.AddMinutes(-1));
        UserPrivacyConsentEvent original = Assert.Single(user.PrivacyConsentEvents);
        context.Users.Attach(user);

        user.RecordPrivacyConsent(purpose, granted: false, PrivacyNoticeVersions.Current, DateTime.UtcNow);
        context.ChangeTracker.DetectChanges();

        UserPrivacyConsentEvent revoked = Assert.Single(user.PrivacyConsentEvents, item => !item.Granted);
        Assert.Multiple(
            () => Assert.Equal(EntityState.Unchanged, context.Entry(original).State),
            () => Assert.Equal(EntityState.Added, context.Entry(revoked).State));
    }

    private static UsersDbContext CreateContext() => new(new DbContextOptionsBuilder<UsersDbContext>()
        .UseNpgsql("Host=localhost;Database=consent-tracking")
        .Options);
}
