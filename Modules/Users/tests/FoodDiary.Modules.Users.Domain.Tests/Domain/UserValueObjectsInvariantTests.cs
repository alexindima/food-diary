using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class UserValueObjectsInvariantTests {
    [Fact]
    public void UserAccountState_CreateInitial_ReturnsActiveState() {
        var state = UserAccountState.CreateInitial();

        Assert.Multiple(
            () => Assert.True(state.IsActive),
            () => Assert.Null(state.TelegramUserId),
            () => Assert.Null(state.DeletedAt));
    }

    [Fact]
    public void UserProfileState_CreateInitial_ExposesProjectionStates() {
        var state = UserProfileState.CreateInitial();

        Assert.Multiple(
            () => Assert.Equal(ActivityLevel.Moderate, state.ActivityLevel),
            () => Assert.Equal(ThemeCode.Default.Value, state.Theme),
            () => Assert.Equal(UiStyleCode.Default.Value, state.UiStyle),
            () => Assert.False(state.PushNotificationsEnabled),
            () => Assert.True(state.FastingPushNotificationsEnabled),
            () => Assert.True(state.SocialPushNotificationsEnabled),
            () => Assert.Equal(12, state.FastingCheckInReminderHours),
            () => Assert.Equal(20, state.FastingCheckInFollowUpReminderHours),
            () => Assert.Equal(state.Username, state.PersonalInfo.Username),
            () => Assert.Equal(state.ProfileImage, state.Media.ProfileImage),
            () => Assert.Equal(state.DashboardLayoutJson, state.Preferences.DashboardLayoutJson));
    }

    [Fact]
    public void UserAccountState_WithTelegram_SetsTelegramUserId() {
        UserAccountState state = UserAccountState.CreateInitial().WithTelegram(12345);

        Assert.Equal(12345, state.TelegramUserId);
    }

    [Fact]
    public void UserAccountState_Deactivate_SetsInactive() {
        UserAccountState state = UserAccountState.CreateInitial().Deactivate();

        Assert.False(state.IsActive);
    }

    [Fact]
    public void UserAccountState_Activate_SetsActive() {
        UserAccountState state = UserAccountState.CreateInitial()
            .Deactivate()
            .Activate();

        Assert.True(state.IsActive);
    }

    [Fact]
    public void UserAccountState_MarkDeleted_SetsDeletedAtAndInactive() {
        DateTime deletedAt = DateTime.UtcNow;
        UserAccountState state = UserAccountState.CreateInitial().MarkDeleted(deletedAt);

        Assert.Equal(deletedAt, state.DeletedAt);
        Assert.False(state.IsActive);
    }

    [Fact]
    public void UserAccountState_Restore_ClearsDeletedAtAndSetsActive() {
        UserAccountState state = UserAccountState.CreateInitial()
            .MarkDeleted(DateTime.UtcNow)
            .Restore();

        Assert.Null(state.DeletedAt);
        Assert.True(state.IsActive);
    }

    [Fact]
    public void UserAiQuotaState_CreateInitial_WithNegativeInputLimit_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            UserAiQuotaState.CreateInitial(-1, 1000));
    }

    [Fact]
    public void UserAiQuotaState_CreateInitial_WithNegativeOutputLimit_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            UserAiQuotaState.CreateInitial(1000, -1));
    }

    [Fact]
    public void UserAiQuotaState_CreateInitial_WithValidValues_Succeeds() {
        var state = UserAiQuotaState.CreateInitial(100_000, 50_000);

        Assert.Equal(100_000, state.AiInputTokenLimit);
        Assert.Equal(50_000, state.AiOutputTokenLimit);
    }

    [Fact]
    public void UserAiQuotaState_WithLimits_UpdatesOnlyProvidedValues() {
        var state = UserAiQuotaState.CreateInitial(100_000, 50_000);

        UserAiQuotaState updated = state.WithLimits(inputLimit: 200_000, outputLimit: null);

        Assert.Equal(200_000, updated.AiInputTokenLimit);
        Assert.Equal(50_000, updated.AiOutputTokenLimit);
    }

    [Fact]
    public void UserAiQuotaState_WithLimits_WithNegativeInput_Throws() {
        var state = UserAiQuotaState.CreateInitial(100_000, 50_000);

        Assert.Throws<ArgumentOutOfRangeException>(() => state.WithLimits(inputLimit: -1, outputLimit: null));
    }

    [Fact]
    public void UserSecurityState_CreateInitial_SetsPasswordAndDefaults() {
        var state = UserSecurityState.CreateInitial("hashed-password");

        Assert.Multiple(
            () => Assert.Equal("hashed-password", state.Password),
            () => Assert.False(state.IsEmailConfirmed),
            () => Assert.Null(state.EmailConfirmationTokenHash),
            () => Assert.Null(state.PasswordResetTokenHash),
            () => Assert.Null(state.LastLoginAtUtc));
    }

    [Fact]
    public void UserSecurityState_WithPassword_UpdatesPassword() {
        UserSecurityState state = UserSecurityState.CreateInitial("old").WithPassword("new");

        Assert.Equal("new", state.Password);
    }

    [Fact]
    public void UserSecurityState_AsEmailConfirmed_ClearsConfirmationTokens() {
        UserSecurityState state = UserSecurityState.CreateInitial("hash")
            .WithEmailConfirmationToken("token-hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow)
            .AsEmailConfirmed(isConfirmed: true);

        Assert.Multiple(
            () => Assert.True(state.IsEmailConfirmed),
            () => Assert.Null(state.EmailConfirmationTokenHash),
            () => Assert.Null(state.EmailConfirmationTokenExpiresAtUtc),
            () => Assert.Null(state.EmailConfirmationSentAtUtc));
    }

    [Fact]
    public void UserSecurityState_WithPasswordResetToken_SetsAllFields() {
        DateTime expires = DateTime.UtcNow.AddHours(1);
        DateTime now = DateTime.UtcNow;
        UserSecurityState state = UserSecurityState.CreateInitial("hash")
            .WithPasswordResetToken("reset-hash", expires, now);

        Assert.Multiple(
            () => Assert.Equal("reset-hash", state.PasswordResetTokenHash),
            () => Assert.Equal(expires, state.PasswordResetTokenExpiresAtUtc),
            () => Assert.Equal(now, state.PasswordResetSentAtUtc));
    }

    [Fact]
    public void UserSecurityState_WithoutPasswordResetToken_ClearsResetFields() {
        UserSecurityState state = UserSecurityState.CreateInitial("hash")
            .WithPasswordResetToken("hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow)
            .WithoutPasswordResetToken();

        Assert.Multiple(
            () => Assert.Null(state.PasswordResetTokenHash),
            () => Assert.Null(state.PasswordResetTokenExpiresAtUtc),
            () => Assert.Null(state.PasswordResetSentAtUtc));
    }

    [Fact]
    public void UserSecurityState_WithoutTransientTokens_ClearsAllTransientState() {
        UserSecurityState state = UserSecurityState.CreateInitial("hash")
            .WithEmailConfirmationToken("confirm", DateTime.UtcNow.AddHours(1), DateTime.UtcNow)
            .WithPasswordResetToken("reset", DateTime.UtcNow.AddHours(1), DateTime.UtcNow)
            .WithoutTransientTokens();

        Assert.Multiple(
            () => Assert.Null(state.EmailConfirmationTokenHash),
            () => Assert.Null(state.EmailConfirmationTokenExpiresAtUtc),
            () => Assert.Null(state.EmailConfirmationSentAtUtc),
            () => Assert.Null(state.PasswordResetTokenHash),
            () => Assert.Null(state.PasswordResetTokenExpiresAtUtc),
            () => Assert.Null(state.PasswordResetSentAtUtc));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void UserNutritionGoals_With_WithNonFiniteValue_Throws(double value) {
        var goals = UserNutritionGoals.Create(2000, 120, 70, 230, 30, 2.5);

        Assert.Throws<ArgumentOutOfRangeException>(() => goals.With(fatTarget: value));
    }

    [Fact]
    public void UserNutritionGoals_With_WithNegativeValue_Throws() {
        var goals = UserNutritionGoals.Create(2000, 120, 70, 230, 30, 2.5);

        Assert.Throws<ArgumentOutOfRangeException>(() => goals.With(carbTarget: -1));
    }

    [Fact]
    public void UserNutritionGoals_Create_WithNegativeValue_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            UserNutritionGoals.Create(-1, proteinTarget: null, fatTarget: null, carbTarget: null, fiberTarget: null, waterGoal: null));
    }

    [Fact]
    public void UserActivityGoals_Create_WithNegativeStepGoal_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            UserActivityGoals.Create(-1, hydrationGoal: null));
    }

    [Fact]
    public void UserActivityGoals_Create_WithNegativeHydrationGoal_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            UserActivityGoals.Create(stepGoal: null, -1));
    }

    [Theory]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void UserActivityGoals_Create_WithInfiniteHydrationGoal_Throws(double value) {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            UserActivityGoals.Create(stepGoal: null, value));
    }

    [Fact]
    public void UserActivityGoals_With_UpdatesOnlyProvidedValues() {
        var goals = UserActivityGoals.Create(10000, 2.5);

        UserActivityGoals updated = goals.With(stepGoal: 12000);

        Assert.Equal(12000, updated.StepGoal);
        Assert.Equal(2.5, updated.HydrationGoal);
    }

    [Fact]
    public void UserActivityGoals_With_WithNegativeStepGoal_Throws() {
        var goals = UserActivityGoals.Create(10000, 2.5);

        Assert.Throws<ArgumentOutOfRangeException>(() => goals.With(stepGoal: -1));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void UserActivityGoals_With_WithNonFiniteHydration_Throws(double value) {
        var goals = UserActivityGoals.Create(10000, 2.5);

        Assert.Throws<ArgumentOutOfRangeException>(() => goals.With(hydrationGoal: value));
    }

    [Fact]
    public void UserActivityGoals_Create_WithNullValues_Succeeds() {
        var goals = UserActivityGoals.Create(stepGoal: null, hydrationGoal: null);

        Assert.Null(goals.StepGoal);
        Assert.Null(goals.HydrationGoal);
    }

    [Theory]
    [InlineData("m", "M")]
    [InlineData(" f ", "F")]
    [InlineData("O", "O")]
    public void GenderCode_TryParse_WithSupportedValues_Normalizes(string value, string expected) {
        bool parsed = GenderCode.TryParse(value, out GenderCode gender);

        Assert.Multiple(
            () => Assert.True(parsed),
            () => Assert.Equal(expected, gender.Value),
            () => Assert.Equal(expected, gender.ToString()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("x")]
    public void GenderCode_TryParse_WithUnsupportedValues_ReturnsFalse(string? value) {
        bool parsed = GenderCode.TryParse(value, out GenderCode gender);

        Assert.False(parsed);
        Assert.Equal(default, gender);
    }

    [Theory]
    [InlineData(" ocean ", "ocean")]
    [InlineData("LEAF", "leaf")]
    [InlineData("dark", "dark")]
    public void ThemeCode_TryParse_WithSupportedValues_Normalizes(string value, string expected) {
        bool parsed = ThemeCode.TryParse(value, out ThemeCode theme);

        Assert.Multiple(
            () => Assert.True(parsed),
            () => Assert.Equal(expected, theme.Value),
            () => Assert.Equal(expected, theme.ToString()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("light")]
    public void ThemeCode_TryParse_WithUnsupportedValues_ReturnsFalse(string? value) {
        bool parsed = ThemeCode.TryParse(value, out ThemeCode theme);

        Assert.Multiple(
            () => Assert.False(parsed),
            () => Assert.Equal(default, theme),
            () => Assert.Equal("ocean", ThemeCode.Default.Value));
    }

    [Theory]
    [InlineData(" classic ", "classic")]
    [InlineData("MODERN", "modern")]
    public void UiStyleCode_TryParse_WithSupportedValues_Normalizes(string value, string expected) {
        bool parsed = UiStyleCode.TryParse(value, out UiStyleCode style);

        Assert.Multiple(
            () => Assert.True(parsed),
            () => Assert.Equal(expected, style.Value),
            () => Assert.Equal(expected, style.ToString()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("compact")]
    public void UiStyleCode_TryParse_WithUnsupportedValues_ReturnsFalse(string? value) {
        bool parsed = UiStyleCode.TryParse(value, out UiStyleCode style);

        Assert.Multiple(
            () => Assert.False(parsed),
            () => Assert.Equal(default, style),
            () => Assert.Equal("classic", UiStyleCode.Default.Value));
    }
}
