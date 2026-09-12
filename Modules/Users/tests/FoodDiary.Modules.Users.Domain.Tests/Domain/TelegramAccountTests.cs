using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class TelegramAccountTests {
    [Fact]
    public void OidcIdentity_IsStableUntilTelegramIsDisconnected() {
        var user = User.CreateTelegram(123, "hash");
        user.BindTelegramOidcIdentity("https://oauth.telegram.org", "subject-456");
        user.BindTelegramOidcIdentity("https://oauth.telegram.org", "subject-456");
        Assert.Equal(123, user.TelegramUserId);
        Assert.Equal("subject-456", user.TelegramOidcSubject);
        Assert.Throws<InvalidOperationException>(() => user.BindTelegramOidcIdentity("https://oauth.telegram.org", "other"));
        Assert.Throws<InvalidOperationException>(() => user.BindTelegramOidcIdentity("https://other.example", "subject-456"));
        user.LinkGoogleIdentity("https://accounts.google.com", "backup");
        user.UnlinkTelegram();
        Assert.Null(user.TelegramOidcIssuer);
        Assert.Null(user.TelegramOidcSubject);
    }

    [Fact]
    public void TelegramRegistration_HasNoEmailOrPasswordLogin() {
        var user = User.CreateTelegram(123, "inaccessible-hash");

        Assert.Null(user.Email);
        Assert.False(user.HasPassword);
        Assert.False(user.IsEmailConfirmed);
        Assert.Equal(123, user.TelegramUserId);
        Assert.Throws<InvalidOperationException>(() => user.UnlinkTelegram());
        Assert.Throws<InvalidOperationException>(() => user.SetEmailConfirmed(isConfirmed: true));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void TelegramIdentity_MustBePositive(long id) {
        Assert.Throws<ArgumentOutOfRangeException>(() => User.CreateTelegram(id, "hash"));
        var user = User.Create("valid@example.com", "hash");
        Assert.Throws<ArgumentOutOfRangeException>(() => user.LinkTelegram(id));
    }

    [Fact]
    public void VerifiedEmailAlone_DoesNotPermitDisconnectingLastLogin() {
        var user = User.CreateTelegram(123, "hash");
        user.AddVerifiedEmail("backup@example.com");
        Assert.Throws<InvalidOperationException>(() => user.UnlinkTelegram());

        user.UpdatePassword("new-hash");
        long version = user.SecurityVersion;
        user.UnlinkTelegram();
        Assert.Null(user.TelegramUserId);
        Assert.Equal(version + 1, user.SecurityVersion);
        user.UnlinkTelegram();
        Assert.Equal(version + 1, user.SecurityVersion);
    }

    [Fact]
    public void GoogleIdentity_AllowsDisconnectingTelegramWithoutEmail() {
        var user = User.CreateTelegram(123, "hash");
        user.LinkGoogleIdentity("https://accounts.google.com", "subject");
        user.UnlinkTelegram();
        Assert.Null(user.TelegramUserId);
        Assert.Null(user.Email);
    }

    [Fact]
    public void TelegramLink_IsIdempotentAndCannotReplaceAnotherIdentity() {
        var user = User.Create("owner@example.com", "hash");
        user.LinkTelegram(123);
        long version = user.SecurityVersion;
        user.LinkTelegram(123);
        Assert.Equal(version, user.SecurityVersion);
        Assert.Throws<InvalidOperationException>(() => user.LinkTelegram(456));
        Assert.Equal(123, user.TelegramUserId);
    }

    [Fact]
    public void TimeZone_PreservesIanaZoneAndRejectsUnknownValue() {
        var user = User.CreateTelegram(123, "hash");
        user.SetTimeZone("Europe/Berlin");
        Assert.Equal("Europe/Berlin", user.TimeZoneId);
        Assert.Throws<TimeZoneNotFoundException>(() => user.SetTimeZone("Invalid/Zone"));
    }
}
