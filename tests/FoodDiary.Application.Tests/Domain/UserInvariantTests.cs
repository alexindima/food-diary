using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Events;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Tests.Domain;

public class UserInvariantTests {
    [Fact]
    public void Create_WithEmptyEmail_Throws() {
        Assert.Throws<ArgumentException>(() => User.Create("   ", "hash"));
    }

    [Fact]
    public void Create_WithEmptyPassword_Throws() {
        Assert.Throws<ArgumentException>(() => User.Create("test@example.com", "   "));
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

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void SetPasswordResetToken_WithEmptyHash_Throws(string tokenHash) {
        var user = User.Create("test@example.com", "hash");

        Assert.Throws<ArgumentException>(() =>
            user.SetPasswordResetToken(tokenHash, DateTime.UtcNow.AddMinutes(30)));
    }

    [Fact]
    public void SetEmailConfirmationToken_WithValidData_UpdatesFields() {
        var user = User.Create("test@example.com", "hash");
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(30);

        user.SetEmailConfirmationToken(" token-hash ", expiresAtUtc);

        Assert.Equal("token-hash", user.EmailConfirmationTokenHash);
        Assert.Equal(expiresAtUtc, user.EmailConfirmationTokenExpiresAtUtc);
        Assert.NotNull(user.EmailConfirmationSentAtUtc);
    }

    [Fact]
    public void SetEmailConfirmationToken_WithLocalExpiry_NormalizesToUtc() {
        var user = User.Create("test@example.com", "hash");
        var expiresAtLocal = DateTime.Now.AddMinutes(30);

        user.SetEmailConfirmationToken("token-hash", expiresAtLocal);

        Assert.Equal(expiresAtLocal.ToUniversalTime(), user.EmailConfirmationTokenExpiresAtUtc);
        Assert.Equal(DateTimeKind.Utc, user.EmailConfirmationTokenExpiresAtUtc!.Value.Kind);
    }

    [Fact]
    public void SetEmailConfirmationToken_WithUnspecifiedExpiry_Throws() {
        var user = User.Create("test@example.com", "hash");
        var expiresAtUnspecified = DateTime.SpecifyKind(DateTime.UtcNow.AddMinutes(30), DateTimeKind.Unspecified);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.SetEmailConfirmationToken("token-hash", expiresAtUnspecified));
    }

    [Fact]
    public void ConfirmEmail_ClearsEmailConfirmationTokenFields() {
        var user = User.Create("test@example.com", "hash");
        user.SetEmailConfirmationToken("token-hash", DateTime.UtcNow.AddMinutes(30));

        user.ConfirmEmail();

        Assert.True(user.IsEmailConfirmed);
        Assert.Null(user.EmailConfirmationTokenHash);
        Assert.Null(user.EmailConfirmationTokenExpiresAtUtc);
        Assert.Null(user.EmailConfirmationSentAtUtc);
    }

    [Fact]
    public void SetPasswordResetToken_WithValidData_UpdatesFields() {
        var user = User.Create("test@example.com", "hash");
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(30);

        user.SetPasswordResetToken(" reset-hash ", expiresAtUtc);

        Assert.Equal("reset-hash", user.PasswordResetTokenHash);
        Assert.Equal(expiresAtUtc, user.PasswordResetTokenExpiresAtUtc);
        Assert.NotNull(user.PasswordResetSentAtUtc);
    }

    [Fact]
    public void SetPasswordResetToken_WithLocalExpiry_NormalizesToUtc() {
        var user = User.Create("test@example.com", "hash");
        var expiresAtLocal = DateTime.Now.AddMinutes(30);

        user.SetPasswordResetToken("reset-hash", expiresAtLocal);

        Assert.Equal(expiresAtLocal.ToUniversalTime(), user.PasswordResetTokenExpiresAtUtc);
        Assert.Equal(DateTimeKind.Utc, user.PasswordResetTokenExpiresAtUtc!.Value.Kind);
    }

    [Fact]
    public void SetPasswordResetToken_WithUnspecifiedExpiry_Throws() {
        var user = User.Create("test@example.com", "hash");
        var expiresAtUnspecified = DateTime.SpecifyKind(DateTime.UtcNow.AddMinutes(30), DateTimeKind.Unspecified);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.SetPasswordResetToken("reset-hash", expiresAtUnspecified));
    }

    [Fact]
    public void ClearPasswordResetToken_ClearsFields() {
        var user = User.Create("test@example.com", "hash");
        user.SetPasswordResetToken("reset-hash", DateTime.UtcNow.AddMinutes(30));

        user.ClearPasswordResetToken();

        Assert.Null(user.PasswordResetTokenHash);
        Assert.Null(user.PasswordResetTokenExpiresAtUtc);
        Assert.Null(user.PasswordResetSentAtUtc);
    }

    [Fact]
    public void UpdateProfile_WithFutureBirthDate_Throws() {
        var user = User.Create("test@example.com", "hash");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.UpdateProfile(new UserProfileUpdate(BirthDate: DateTime.UtcNow.AddDays(1))));
    }

    [Fact]
    public void UpdateProfile_WithNegativeStepGoal_Throws() {
        var user = User.Create("test@example.com", "hash");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.UpdateProfile(new UserProfileUpdate(StepGoal: -1)));
    }

    [Fact]
    public void UpdateProfile_WithNegativeHydrationGoal_Throws() {
        var user = User.Create("test@example.com", "hash");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.UpdateProfile(new UserProfileUpdate(HydrationGoal: -0.1)));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void UpdateProfile_WithNonFiniteWeight_Throws(double weight) {
        var user = User.Create("test@example.com", "hash");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.UpdateProfile(new UserProfileUpdate(Weight: weight)));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void UpdateProfile_WithNonFiniteHeight_Throws(double height) {
        var user = User.Create("test@example.com", "hash");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.UpdateProfile(new UserProfileUpdate(Height: height)));
    }

    [Fact]
    public void UpdateProfile_WithPartialActivityGoalUpdate_PreservesOtherValue() {
        var user = User.Create("test@example.com", "hash");
        user.UpdateProfile(new UserProfileUpdate(StepGoal: 8000, HydrationGoal: 2.2));

        user.UpdateProfile(new UserProfileUpdate(StepGoal: 10000));

        Assert.Equal(10000, user.StepGoal);
        Assert.Equal(2.2, user.HydrationGoal);
    }

    [Fact]
    public void UpdateActivity_WithPartialUpdate_PreservesOtherValue() {
        var user = User.Create("test@example.com", "hash");
        user.UpdateActivity(stepGoal: 8000, hydrationGoal: 2.2);

        user.UpdateActivity(stepGoal: 10000);

        Assert.Equal(10000, user.StepGoal);
        Assert.Equal(2.2, user.HydrationGoal);
    }

    [Fact]
    public void UpdateGoals_WithNegativeCalorieTarget_Throws() {
        var user = User.Create("test@example.com", "hash");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.UpdateGoals(dailyCalorieTarget: -1));
    }

    [Theory]
    [InlineData(-1d, null, null, null, null, null)]
    [InlineData(null, -1d, null, null, null, null)]
    [InlineData(null, null, -1d, null, null, null)]
    [InlineData(null, null, null, -1d, null, null)]
    [InlineData(null, null, null, null, -1d, null)]
    [InlineData(null, null, null, null, null, -0.1d)]
    public void UpdateGoals_WithNegativeNutritionValue_Throws(
        double? dailyCalorieTarget,
        double? proteinTarget,
        double? fatTarget,
        double? carbTarget,
        double? fiberTarget,
        double? waterGoal) {
        var user = User.Create("test@example.com", "hash");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.UpdateGoals(
                dailyCalorieTarget: dailyCalorieTarget,
                proteinTarget: proteinTarget,
                fatTarget: fatTarget,
                carbTarget: carbTarget,
                fiberTarget: fiberTarget,
                waterGoal: waterGoal));
    }

    [Fact]
    public void UpdateGoals_WithValidValues_UpdatesTargets() {
        var user = User.Create("test@example.com", "hash");

        user.UpdateGoals(
            dailyCalorieTarget: 2200,
            proteinTarget: 140,
            fatTarget: 80,
            carbTarget: 240,
            fiberTarget: 30,
            waterGoal: 2.5,
            desiredWeight: 74.5,
            desiredWaist: 88);

        Assert.Equal(2200, user.DailyCalorieTarget);
        Assert.Equal(140, user.ProteinTarget);
        Assert.Equal(80, user.FatTarget);
        Assert.Equal(240, user.CarbTarget);
        Assert.Equal(30, user.FiberTarget);
        Assert.Equal(2.5, user.WaterGoal);
        Assert.Equal(74.5, user.DesiredWeight);
        Assert.Equal(88, user.DesiredWaist);
    }

    [Fact]
    public void UpdateGoals_WithPartialNutritionUpdate_PreservesOtherValues() {
        var user = User.Create("test@example.com", "hash");
        user.UpdateGoals(
            dailyCalorieTarget: 2200,
            proteinTarget: 140,
            fatTarget: 80,
            carbTarget: 240,
            fiberTarget: 30,
            waterGoal: 2.5);

        user.UpdateGoals(proteinTarget: 150);

        Assert.Equal(2200, user.DailyCalorieTarget);
        Assert.Equal(150, user.ProteinTarget);
        Assert.Equal(80, user.FatTarget);
        Assert.Equal(240, user.CarbTarget);
        Assert.Equal(30, user.FiberTarget);
        Assert.Equal(2.5, user.WaterGoal);
    }

    [Theory]
    [InlineData(-1d)]
    [InlineData(0d)]
    [InlineData(500.0001d)]
    public void UpdateDesiredWeight_WithInvalidValue_Throws(double desiredWeight) {
        var user = User.Create("test@example.com", "hash");

        Assert.Throws<ArgumentOutOfRangeException>(() => user.UpdateDesiredWeight(desiredWeight));
    }

    [Theory]
    [InlineData(0.0001d)]
    [InlineData(500d)]
    public void UpdateDesiredWeight_WithBoundaryValues_UpdatesValue(double desiredWeight) {
        var user = User.Create("test@example.com", "hash");

        user.UpdateDesiredWeight(desiredWeight);

        Assert.Equal(desiredWeight, user.DesiredWeight);
    }

    [Theory]
    [InlineData(-1d)]
    [InlineData(0d)]
    [InlineData(300.0001d)]
    public void UpdateDesiredWaist_WithInvalidValue_Throws(double desiredWaist) {
        var user = User.Create("test@example.com", "hash");

        Assert.Throws<ArgumentOutOfRangeException>(() => user.UpdateDesiredWaist(desiredWaist));
    }

    [Theory]
    [InlineData(0.0001d)]
    [InlineData(300d)]
    public void UpdateDesiredWaist_WithBoundaryValues_UpdatesValue(double desiredWaist) {
        var user = User.Create("test@example.com", "hash");

        user.UpdateDesiredWaist(desiredWaist);

        Assert.Equal(desiredWaist, user.DesiredWaist);
    }

    [Fact]
    public void UpdateDesiredWeight_WithNull_ClearsValue() {
        var user = User.Create("test@example.com", "hash");
        user.UpdateDesiredWeight(80);

        user.UpdateDesiredWeight(null);

        Assert.Null(user.DesiredWeight);
    }

    [Fact]
    public void UpdateDesiredWaist_WithNull_ClearsValue() {
        var user = User.Create("test@example.com", "hash");
        user.UpdateDesiredWaist(90);

        user.UpdateDesiredWaist(null);

        Assert.Null(user.DesiredWaist);
    }

    [Fact]
    public void UpdateProfile_WithUnsupportedLanguage_Throws() {
        var user = User.Create("test@example.com", "hash");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.UpdateProfile(new UserProfileUpdate(Language: "de")));
    }

    [Fact]
    public void UpdateProfile_WithSupportedLanguage_UpdatesValue() {
        var user = User.Create("test@example.com", "hash");

        user.UpdateProfile(new UserProfileUpdate(Language: "ru"));

        Assert.Equal("ru", user.Language);
    }

    [Fact]
    public void UpdatePreferences_WithSupportedLanguage_UpdatesValue() {
        var user = User.Create("test@example.com", "hash");

        user.UpdatePreferences(language: "ru");

        Assert.Equal("ru", user.Language);
    }

    [Fact]
    public void UpdateProfile_WithUnsupportedGender_Throws() {
        var user = User.Create("test@example.com", "hash");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.UpdateProfile(new UserProfileUpdate(Gender: "X")));
    }

    [Fact]
    public void UpdateProfile_WithSupportedGender_NormalizesToCanonicalCode() {
        var user = User.Create("test@example.com", "hash");

        user.UpdateProfile(new UserProfileUpdate(Gender: "f"));

        Assert.Equal("F", user.Gender);
    }

    [Fact]
    public void UpdatePersonalInfo_WithSupportedGender_NormalizesToCanonicalCode() {
        var user = User.Create("test@example.com", "hash");

        user.UpdatePersonalInfo(gender: "f");

        Assert.Equal("F", user.Gender);
    }

    [Fact]
    public void UpdateProfileMedia_WithValues_UpdatesImageFields() {
        var user = User.Create("test@example.com", "hash");
        var assetId = FoodDiary.Domain.ValueObjects.Ids.ImageAssetId.New();

        user.UpdateProfileMedia(profileImage: " https://cdn.example.com/avatar.webp ", profileImageAssetId: assetId);

        Assert.Equal("https://cdn.example.com/avatar.webp", user.ProfileImage);
        Assert.Equal(assetId, user.ProfileImageAssetId);
    }

    [Fact]
    public void MarkDeleted_SetsDeletedState_AndRaisesDomainEvent() {
        var user = User.Create("test@example.com", "hash");
        var deletedAt = DateTime.UtcNow;

        user.MarkDeleted(deletedAt);

        Assert.False(user.IsActive);
        Assert.Equal(deletedAt, user.DeletedAt);
        Assert.Single(user.DomainEvents);
        Assert.IsType<UserDeletedDomainEvent>(user.DomainEvents[0]);
    }

    [Fact]
    public void MarkDeleted_WithLocalTimestamp_NormalizesToUtc() {
        var user = User.Create("test@example.com", "hash");
        var deletedAtLocal = DateTime.Now;

        user.MarkDeleted(deletedAtLocal);

        Assert.Equal(deletedAtLocal.ToUniversalTime(), user.DeletedAt);
        Assert.Equal(DateTimeKind.Utc, user.DeletedAt!.Value.Kind);
        Assert.Equal(deletedAtLocal.ToUniversalTime(), ((UserDeletedDomainEvent)user.DomainEvents[0]).DeletedAtUtc);
    }

    [Fact]
    public void MarkDeleted_WithUnspecifiedTimestamp_Throws() {
        var user = User.Create("test@example.com", "hash");
        var deletedAtUnspecified = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

        Assert.Throws<ArgumentOutOfRangeException>(() => user.MarkDeleted(deletedAtUnspecified));
    }

    [Fact]
    public void MarkDeleted_WhenAlreadyDeletedAndInactive_IsIdempotent() {
        var user = User.Create("test@example.com", "hash");
        user.MarkDeleted(DateTime.UtcNow);
        var initialEventCount = user.DomainEvents.Count;

        user.MarkDeleted(DateTime.UtcNow.AddMinutes(1));

        Assert.Equal(initialEventCount, user.DomainEvents.Count);
    }

    [Fact]
    public void Restore_WhenDeleted_SetsActiveAndRaisesDomainEvent() {
        var user = User.Create("test@example.com", "hash");
        user.MarkDeleted(DateTime.UtcNow);
        user.ClearDomainEvents();

        user.Restore();

        Assert.True(user.IsActive);
        Assert.Null(user.DeletedAt);
        Assert.Single(user.DomainEvents);
        Assert.IsType<UserRestoredDomainEvent>(user.DomainEvents[0]);
    }

    [Fact]
    public void Restore_WhenAlreadyActiveAndNotDeleted_IsIdempotent() {
        var user = User.Create("test@example.com", "hash");

        user.Restore();

        Assert.Empty(user.DomainEvents);
        Assert.True(user.IsActive);
        Assert.Null(user.DeletedAt);
    }

    [Fact]
    public void UpdatePassword_WhenDeleted_Throws() {
        var user = User.Create("test@example.com", "hash");
        user.MarkDeleted(DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() => user.UpdatePassword("new-hash"));
    }

    [Fact]
    public void UpdateProfile_WhenDeleted_Throws() {
        var user = User.Create("test@example.com", "hash");
        user.MarkDeleted(DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            user.UpdateProfile(new UserProfileUpdate(FirstName: "Alex")));
    }

    [Fact]
    public void SetPasswordResetToken_WhenDeleted_Throws() {
        var user = User.Create("test@example.com", "hash");
        user.MarkDeleted(DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            user.SetPasswordResetToken("reset-hash", DateTime.UtcNow.AddMinutes(30)));
    }
}
