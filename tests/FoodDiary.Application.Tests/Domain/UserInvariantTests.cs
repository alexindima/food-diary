using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Tests.Domain;

public class UserInvariantTests {
    [Fact]
    public void Create_WithEmptyEmail_Throws() {
        Assert.Throws<ArgumentException>(() => User.Create("   ", "hash"));
    }

    [Fact]
    public void UpdateAiTokenLimits_WithNegativeInput_Throws() {
        var user = User.Create("test@example.com", "hash");

        Assert.Throws<ArgumentOutOfRangeException>(() => user.UpdateAiTokenLimits(-1, null));
    }

    [Fact]
    public void UpdateAiTokenLimits_WithNegativeOutput_Throws() {
        var user = User.Create("test@example.com", "hash");

        Assert.Throws<ArgumentOutOfRangeException>(() => user.UpdateAiTokenLimits(null, -1));
    }

    [Fact]
    public void UpdateRefreshToken_WithNull_DoesNotSetLastLogin() {
        var user = User.Create("test@example.com", "hash");

        user.UpdateRefreshToken(null);

        Assert.Null(user.LastLoginAtUtc);
    }

    [Fact]
    public void UpdateRefreshToken_WithToken_SetsLastLogin() {
        var user = User.Create("test@example.com", "hash");

        user.UpdateRefreshToken("token");

        Assert.NotNull(user.LastLoginAtUtc);
    }

    [Fact]
    public void Activate_WhenDeleted_Throws() {
        var user = User.Create("test@example.com", "hash");
        user.MarkDeleted(DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() => user.Activate());
    }

    [Fact]
    public void SetEmailConfirmationToken_WithPastExpiry_Throws() {
        var user = User.Create("test@example.com", "hash");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.SetEmailConfirmationToken("hash", DateTime.UtcNow.AddMinutes(-1)));
    }

    [Fact]
    public void SetPasswordResetToken_WithEmptyHash_Throws() {
        var user = User.Create("test@example.com", "hash");

        Assert.Throws<ArgumentException>(() =>
            user.SetPasswordResetToken("   ", DateTime.UtcNow.AddMinutes(30)));
    }

    [Fact]
    public void UpdateProfile_WithFutureBirthDate_Throws() {
        var user = User.Create("test@example.com", "hash");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.UpdateProfile(birthDate: DateTime.UtcNow.AddDays(1)));
    }

    [Fact]
    public void UpdateProfile_WithNegativeCalorieTarget_Throws() {
        var user = User.Create("test@example.com", "hash");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.UpdateProfile(dailyCalorieTarget: -1));
    }

    [Fact]
    public void UpdateDesiredWeight_WithZero_Throws() {
        var user = User.Create("test@example.com", "hash");

        Assert.Throws<ArgumentOutOfRangeException>(() => user.UpdateDesiredWeight(0));
    }

    [Fact]
    public void UpdateDesiredWeight_WithTooLargeValue_Throws() {
        var user = User.Create("test@example.com", "hash");

        Assert.Throws<ArgumentOutOfRangeException>(() => user.UpdateDesiredWeight(501));
    }

    [Fact]
    public void UpdateDesiredWaist_WithTooLargeValue_Throws() {
        var user = User.Create("test@example.com", "hash");

        Assert.Throws<ArgumentOutOfRangeException>(() => user.UpdateDesiredWaist(301));
    }

    [Fact]
    public void UpdateProfile_WithUnsupportedLanguage_Throws() {
        var user = User.Create("test@example.com", "hash");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.UpdateProfile(language: "de"));
    }

    [Fact]
    public void UpdateProfile_WithSupportedLanguage_UpdatesValue() {
        var user = User.Create("test@example.com", "hash");

        user.UpdateProfile(language: "ru");

        Assert.Equal("ru", user.Language);
    }

    [Fact]
    public void UpdateProfile_WithUnsupportedGender_Throws() {
        var user = User.Create("test@example.com", "hash");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.UpdateProfile(gender: "X"));
    }

    [Fact]
    public void UpdateProfile_WithSupportedGender_NormalizesToCanonicalCode() {
        var user = User.Create("test@example.com", "hash");

        user.UpdateProfile(gender: "f");

        Assert.Equal("F", user.Gender);
    }
}
