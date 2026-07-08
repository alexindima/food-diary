using System.Reflection;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.Events;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
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
    public void RefreshTokenSession_Create_WithEmptySessionId_Throws() {
        Assert.Throws<ArgumentException>(() => UserRefreshTokenSession.Create(
            Guid.Empty,
            UserId.New(),
            "hash",
            rememberMe: false,
            authProvider: null,
            ipAddress: null,
            userAgent: null,
            DateTime.UtcNow));
    }

    [Fact]
    public void RefreshTokenSession_Create_WithEmptyUserId_Throws() {
        Assert.Throws<ArgumentException>(() => UserRefreshTokenSession.Create(
            Guid.NewGuid(),
            UserId.Empty,
            "hash",
            rememberMe: false,
            authProvider: null,
            ipAddress: null,
            userAgent: null,
            DateTime.UtcNow));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void RefreshTokenSession_Create_WithBlankRefreshTokenHash_Throws(string refreshTokenHash) {
        Assert.Throws<ArgumentException>(() => UserRefreshTokenSession.Create(
            Guid.NewGuid(),
            UserId.New(),
            refreshTokenHash,
            rememberMe: false,
            authProvider: null,
            ipAddress: null,
            userAgent: null,
            DateTime.UtcNow));
    }

    [Fact]
    public void RefreshTokenSession_Rotate_WhenRevoked_Throws() {
        UserRefreshTokenSession session = CreateRefreshTokenSession();
        session.Revoke(DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            session.Rotate("next-hash", rememberMe: true, DateTime.UtcNow.AddMinutes(1), TimeSpan.FromMinutes(5)));
    }

    [Fact]
    public void RefreshTokenSession_Revoke_WhenActive_ClearsPreviousTokenState() {
        UserRefreshTokenSession session = CreateRefreshTokenSession();
        DateTime rotatedAtUtc = DateTime.UtcNow.AddMinutes(1);
        session.Rotate("rotated-hash", rememberMe: true, rotatedAtUtc, TimeSpan.FromMinutes(5));
        DateTime revokedAtUtc = rotatedAtUtc.AddMinutes(1);

        session.Revoke(revokedAtUtc);

        Assert.Multiple(
            () => Assert.False(session.IsActive),
            () => Assert.Equal(revokedAtUtc, session.RevokedAtUtc),
            () => Assert.Null(session.PreviousRefreshTokenHash),
            () => Assert.Null(session.PreviousRefreshTokenValidUntilUtc),
            () => Assert.Equal(revokedAtUtc, session.ModifiedOnUtc));
    }

    [Fact]
    public void RefreshTokenSession_Revoke_WhenAlreadyRevoked_DoesNotChangeTimestamp() {
        UserRefreshTokenSession session = CreateRefreshTokenSession();
        DateTime revokedAtUtc = DateTime.UtcNow.AddMinutes(1);
        session.Revoke(revokedAtUtc);

        session.Revoke(revokedAtUtc.AddMinutes(5));

        Assert.Equal(revokedAtUtc, session.RevokedAtUtc);
        Assert.Equal(revokedAtUtc, session.ModifiedOnUtc);
    }

    [Fact]
    public void NavigationCollections_AreExposedAsReadOnly() {
        var user = User.Create("test@example.com", "hash");

        Assert.Multiple(
            () => Assert.True(Assert.IsAssignableFrom<ICollection<FoodDiary.Domain.Entities.Meals.Meal>>(user.Meals).IsReadOnly),
            () => Assert.True(Assert.IsAssignableFrom<ICollection<FoodDiary.Domain.Entities.Products.Product>>(user.Products).IsReadOnly),
            () => Assert.True(Assert.IsAssignableFrom<ICollection<FoodDiary.Domain.Entities.Recipes.Recipe>>(user.Recipes).IsReadOnly),
            () => Assert.True(Assert.IsAssignableFrom<ICollection<FoodDiary.Domain.Entities.Tracking.WeightEntry>>(user.WeightEntries).IsReadOnly),
            () => Assert.True(Assert.IsAssignableFrom<ICollection<FoodDiary.Domain.Entities.Tracking.WaistEntry>>(user.WaistEntries).IsReadOnly),
            () => Assert.True(Assert.IsAssignableFrom<ICollection<FoodDiary.Domain.Entities.Tracking.CycleProfile>>(user.Cycles).IsReadOnly),
            () => Assert.True(Assert.IsAssignableFrom<ICollection<FoodDiary.Domain.Entities.Tracking.HydrationEntry>>(user.HydrationEntries).IsReadOnly),
            () => Assert.True(Assert.IsAssignableFrom<ICollection<FoodDiary.Domain.Entities.Shopping.ShoppingList>>(user.ShoppingLists).IsReadOnly),
            () => Assert.True(Assert.IsAssignableFrom<ICollection<UserRole>>(user.UserRoles).IsReadOnly));
    }

    [Fact]
    public void ReplaceRoles_AssignsRoleNavigations() {
        var user = User.Create("test@example.com", "hash");
        var adminRole = Role.Create("Admin");
        var supportRole = Role.Create("Support");

        user.ReplaceRoles([adminRole, supportRole]);

        Assert.Collection(
            user.UserRoles.OrderBy(role => role.Role.Name, StringComparer.Ordinal),
            role => {
                Assert.Equal(user.Id, role.UserId);
                Assert.Equal("Admin", role.Role.Name);
            },
            role => {
                Assert.Equal(user.Id, role.UserId);
                Assert.Equal("Support", role.Role.Name);
            });
    }

    [Fact]
    public void GetRoleNames_AndHasRole_UseDistinctNormalizedMembershipView() {
        var user = User.Create("test@example.com", "hash");
        var adminRole = Role.Create("Admin");
        var supportRole = Role.Create("Support");

        user.ReplaceRoles([adminRole, supportRole]);

        Assert.Multiple(
            () => Assert.Equal(["Admin", "Support"], [.. user.GetRoleNames().Order(StringComparer.Ordinal)]),
            () => Assert.True(user.HasRole("Admin")),
            () => Assert.True(user.HasRole(" Support ")),
            () => Assert.False(user.HasRole("Premium")),
            () => Assert.False(user.HasRole(" ")));
    }

    [Fact]
    public void UpdateAiTokenLimits_WithNegativeInput_Throws() {
        var user = User.Create("test@example.com", "hash");

        Assert.Throws<ArgumentOutOfRangeException>(() => user.UpdateAiTokenLimits(-1, outputLimit: null));
    }

    [Fact]
    public void UpdateAiTokenLimits_WithNegativeOutput_Throws() {
        var user = User.Create("test@example.com", "hash");

        Assert.Throws<ArgumentOutOfRangeException>(() => user.UpdateAiTokenLimits(inputLimit: null, -1));
    }

    [Fact]
    public void UpdateAiTokenLimits_WithPrimitiveLimits_UpdatesChangedLimits() {
        var user = User.Create("test@example.com", "hash");

        user.UpdateAiTokenLimits(321, 654);

        Assert.Equal(321, user.AiInputTokenLimit);
        Assert.Equal(654, user.AiOutputTokenLimit);
        Assert.NotNull(user.ModifiedOnUtc);
    }

    [Fact]
    public void UpdateRefreshToken_WithNull_DoesNotSetLastLogin() {
        var user = User.Create("test@example.com", "hash");

        user.UpdateRefreshToken(refreshToken: null);

        Assert.Null(user.LastLoginAtUtc);
    }

    [Fact]
    public void UpdateRefreshToken_WithToken_SetsLastLogin() {
        var user = User.Create("test@example.com", "hash");

        user.UpdateRefreshToken("token");

        Assert.NotNull(user.LastLoginAtUtc);
    }

    [Fact]
    public void UpdateRefreshToken_WithTypedUpdate_SetsLastLogin() {
        var user = User.Create("test@example.com", "hash");

        user.UpdateRefreshToken(new UserRefreshTokenUpdate("token"));

        Assert.NotNull(user.LastLoginAtUtc);
    }

    [Fact]
    public void LinkTelegram_AndUnlinkTelegram_UpdateAccountLinkState() {
        var user = User.Create("test@example.com", "hash");

        user.LinkTelegram(123456789);
        Assert.Equal(123456789, user.TelegramUserId);

        user.UnlinkTelegram();
        Assert.Null(user.TelegramUserId);
    }

    [Fact]
    public void Activate_WhenDeleted_Throws() {
        var user = User.Create("test@example.com", "hash");
        user.MarkDeleted(DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() => user.Activate());
    }

    [Fact]
    public void Activate_WithExplicitTimestamp_ActivatesAndSetsModifiedTimestamp() {
        var user = User.Create("test@example.com", "hash");
        user.Deactivate();
        DateTime activatedAtUtc = DateTime.UtcNow.AddMinutes(5);

        user.Activate(activatedAtUtc);

        Assert.True(user.IsActive);
        Assert.Equal(activatedAtUtc, user.ModifiedOnUtc);
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
        DateTime expiresAtUtc = DateTime.UtcNow.AddMinutes(30);

        user.SetEmailConfirmationToken(" token-hash ", expiresAtUtc);

        Assert.Equal("token-hash", user.EmailConfirmationTokenHash);
        Assert.Equal(expiresAtUtc, user.EmailConfirmationTokenExpiresAtUtc);
        Assert.NotNull(user.EmailConfirmationSentAtUtc);
    }

    [Fact]
    public void SetEmailConfirmationToken_WithTypedIssue_UpdatesFields() {
        var user = User.Create("test@example.com", "hash");
        DateTime expiresAtUtc = DateTime.UtcNow.AddMinutes(30);

        user.SetEmailConfirmationToken(new UserTokenIssue(" token-hash ", expiresAtUtc));

        Assert.Equal("token-hash", user.EmailConfirmationTokenHash);
        Assert.Equal(expiresAtUtc, user.EmailConfirmationTokenExpiresAtUtc);
        Assert.NotNull(user.EmailConfirmationSentAtUtc);
    }

    [Fact]
    public void SetEmailConfirmationToken_WithLocalExpiry_NormalizesToUtc() {
        var user = User.Create("test@example.com", "hash");
        DateTime expiresAtLocal = DateTime.Now.AddMinutes(30);

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
    public void CompleteEmailVerification_ClearsEmailConfirmationTokenFields() {
        var user = User.Create("test@example.com", "hash");
        user.SetEmailConfirmationToken("token-hash", DateTime.UtcNow.AddMinutes(30));

        user.CompleteEmailVerification();

        Assert.Multiple(
            () => Assert.True(user.IsEmailConfirmed),
            () => Assert.Null(user.EmailConfirmationTokenHash),
            () => Assert.Null(user.EmailConfirmationTokenExpiresAtUtc),
            () => Assert.Null(user.EmailConfirmationSentAtUtc));
    }

    [Fact]
    public void SetPasswordResetToken_WithValidData_UpdatesFields() {
        var user = User.Create("test@example.com", "hash");
        DateTime expiresAtUtc = DateTime.UtcNow.AddMinutes(30);

        user.SetPasswordResetToken(" reset-hash ", expiresAtUtc);

        Assert.Equal("reset-hash", user.PasswordResetTokenHash);
        Assert.Equal(expiresAtUtc, user.PasswordResetTokenExpiresAtUtc);
        Assert.NotNull(user.PasswordResetSentAtUtc);
    }

    [Fact]
    public void SetPasswordResetToken_WithTypedIssue_UpdatesFields() {
        var user = User.Create("test@example.com", "hash");
        DateTime expiresAtUtc = DateTime.UtcNow.AddMinutes(30);

        user.SetPasswordResetToken(new UserTokenIssue(" reset-hash ", expiresAtUtc));

        Assert.Equal("reset-hash", user.PasswordResetTokenHash);
        Assert.Equal(expiresAtUtc, user.PasswordResetTokenExpiresAtUtc);
        Assert.NotNull(user.PasswordResetSentAtUtc);
    }

    [Fact]
    public void SetPasswordResetToken_WithLocalExpiry_NormalizesToUtc() {
        var user = User.Create("test@example.com", "hash");
        DateTime expiresAtLocal = DateTime.Now.AddMinutes(30);

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
    public void CompletePasswordReset_UpdatesPasswordAndClearsResetFields() {
        var user = User.Create("test@example.com", "old-hash");
        user.SetPasswordResetToken("reset-hash", DateTime.UtcNow.AddMinutes(30));

        user.CompletePasswordReset("new-hash");

        Assert.Multiple(
            () => Assert.Equal("new-hash", user.Password),
            () => Assert.Null(user.PasswordResetTokenHash),
            () => Assert.Null(user.PasswordResetTokenExpiresAtUtc),
            () => Assert.Null(user.PasswordResetSentAtUtc));
    }

    [Fact]
    public void UpdateProfile_WithFutureBirthDate_Throws() {
        var user = User.Create("test@example.com", "hash");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.UpdatePersonalInfo(new UserPersonalInfoUpdate(BirthDate: DateTime.UtcNow.AddDays(1))));
    }

    [Fact]
    public void UpdateProfile_WithNegativeStepGoal_Throws() {
        var user = User.Create("test@example.com", "hash");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.UpdateActivity(new UserActivityUpdate(StepGoal: -1)));
    }

    [Fact]
    public void UpdateProfile_WithNegativeHydrationGoal_Throws() {
        var user = User.Create("test@example.com", "hash");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.UpdateActivity(new UserActivityUpdate(HydrationGoal: -0.1)));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void UpdateProfile_WithNonFiniteWeight_Throws(double weight) {
        var user = User.Create("test@example.com", "hash");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.UpdatePersonalInfo(new UserPersonalInfoUpdate(Weight: weight)));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void UpdateProfile_WithNonFiniteHeight_Throws(double height) {
        var user = User.Create("test@example.com", "hash");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.UpdatePersonalInfo(new UserPersonalInfoUpdate(Height: height)));
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    public void UpdatePersonalInfo_WithNonPositiveHeight_Throws(double height) {
        var user = User.Create("test@example.com", "hash");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.UpdatePersonalInfo(new UserPersonalInfoUpdate(Height: height)));
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    public void UpdatePersonalInfo_WithNonPositiveWeight_Throws(double weight) {
        var user = User.Create("test@example.com", "hash");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.UpdatePersonalInfo(new UserPersonalInfoUpdate(Weight: weight)));
    }

    [Fact]
    public void UpdatePersonalInfo_WithRemainingProfileFields_UpdatesState() {
        var user = User.Create("test@example.com", "hash");
        DateTime birthDate = DateTime.UtcNow.Date.AddYears(-30);

        user.UpdatePersonalInfo(new UserPersonalInfoUpdate(
            LastName: " Doe ",
            BirthDate: birthDate,
            Weight: 82.5,
            Height: 181.2));

        Assert.Multiple(
            () => Assert.Equal("Doe", user.LastName),
            () => Assert.Equal(birthDate, user.BirthDate),
            () => Assert.Equal(82.5, user.Weight),
            () => Assert.Equal(181.2, user.Height));
        Assert.NotNull(user.ModifiedOnUtc);
    }

    [Fact]
    public void UpdateProfile_WithPartialActivityGoalUpdate_PreservesOtherValue() {
        var user = User.Create("test@example.com", "hash");
        user.UpdateActivity(new UserActivityUpdate(StepGoal: 8000, HydrationGoal: 2.2));

        user.UpdateActivity(new UserActivityUpdate(StepGoal: 10000));

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
    public void UpdateActivity_WithTypedUpdate_PreservesOtherValue() {
        var user = User.Create("test@example.com", "hash");
        user.UpdateActivity(new UserActivityUpdate(StepGoal: 8000, HydrationGoal: 2.2));

        user.UpdateActivity(new UserActivityUpdate(StepGoal: 10000));

        Assert.Equal(10000, user.StepGoal);
        Assert.Equal(2.2, user.HydrationGoal);
    }

    [Fact]
    public void UpdateActivity_WithActivityLevel_UpdatesActivityLevel() {
        var user = User.Create("test@example.com", "hash");

        user.UpdateActivity(activityLevel: ActivityLevel.High);

        Assert.Equal(ActivityLevel.High, user.ActivityLevel);
        Assert.NotNull(user.ModifiedOnUtc);
    }

    [Fact]
    public void UpdateActivity_WithSameValues_DoesNotSetModifiedOnUtc() {
        var user = User.Create("test@example.com", "hash");

        user.UpdateActivity(new UserActivityUpdate(ActivityLevel.Moderate));

        Assert.Null(user.ModifiedOnUtc);
    }

    [Fact]
    public void UpdateActivity_WithSameHydrationGoalWithinTolerance_DoesNotSetModifiedOnUtc() {
        var user = User.Create("test@example.com", "hash");
        user.UpdateActivity(hydrationGoal: 2.2);
        user.ClearDomainEvents();
        DateTime? modifiedOnUtc = user.ModifiedOnUtc;

        user.UpdateActivity(hydrationGoal: 2.2000005);

        Assert.Equal(2.2, user.HydrationGoal);
        Assert.Equal(modifiedOnUtc, user.ModifiedOnUtc);
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

        Assert.Multiple(
            () => Assert.Equal(2200, user.DailyCalorieTarget),
            () => Assert.Equal(140, user.ProteinTarget),
            () => Assert.Equal(80, user.FatTarget),
            () => Assert.Equal(240, user.CarbTarget),
            () => Assert.Equal(30, user.FiberTarget),
            () => Assert.Equal(2.5, user.WaterGoal),
            () => Assert.Equal(74.5, user.DesiredWeight),
            () => Assert.Equal(88, user.DesiredWaist));
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

        Assert.Multiple(
            () => Assert.Equal(2200, user.DailyCalorieTarget),
            () => Assert.Equal(150, user.ProteinTarget),
            () => Assert.Equal(80, user.FatTarget),
            () => Assert.Equal(240, user.CarbTarget),
            () => Assert.Equal(30, user.FiberTarget),
            () => Assert.Equal(2.5, user.WaterGoal));
    }

    [Fact]
    public void GetCalorieTargets_WhenCyclingDisabled_UsesDailyTarget() {
        var user = User.Create("test@example.com", "hash");
        user.UpdateGoals(dailyCalorieTarget: 2100);

        Assert.Equal(2100, user.GetCalorieTargetForDate(new DateTime(2026, 6, 1)));
        Assert.Equal(14700, user.GetWeeklyCalorieTarget());
    }

    [Fact]
    public void GetCalorieTargets_WhenCyclingEnabled_UsesPerDayOverridesAndFallbacks() {
        var user = User.Create("test@example.com", "hash");

        user.UpdateGoals(new UserGoalUpdate(
            DailyCalorieTarget: 2000,
            CalorieCyclingEnabled: true,
            MondayCalories: 1800,
            TuesdayCalories: 1900,
            WednesdayCalories: 2000,
            ThursdayCalories: 2100,
            FridayCalories: 2200,
            SaturdayCalories: 2300));

        Assert.Multiple(
            () => Assert.Equal(1800, user.GetCalorieTargetForDate(new DateTime(2026, 6, 1))),
            () => Assert.Equal(1900, user.GetCalorieTargetForDate(new DateTime(2026, 6, 2))),
            () => Assert.Equal(2000, user.GetCalorieTargetForDate(new DateTime(2026, 6, 3))),
            () => Assert.Equal(2100, user.GetCalorieTargetForDate(new DateTime(2026, 6, 4))),
            () => Assert.Equal(2200, user.GetCalorieTargetForDate(new DateTime(2026, 6, 5))),
            () => Assert.Equal(2300, user.GetCalorieTargetForDate(new DateTime(2026, 6, 6))),
            () => Assert.Equal(2000, user.GetCalorieTargetForDate(new DateTime(2026, 6, 7))),
            () => Assert.Equal(14300, user.GetWeeklyCalorieTarget()));
    }

    [Fact]
    public void GetCalorieTargets_WhenCyclingEnabledWithoutDailyTarget_ReturnsNullFallbackAndZeroWeeklyTarget() {
        var user = User.Create("test@example.com", "hash");

        user.UpdateGoals(new UserGoalUpdate(CalorieCyclingEnabled: true));

        Assert.Null(user.GetCalorieTargetForDate(new DateTime(2026, 6, 7)));
        Assert.Equal(0, user.GetWeeklyCalorieTarget());
    }

    [Theory]
    [InlineData(-1d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void UpdateGoals_WithInvalidDayCalories_Throws(double mondayCalories) {
        var user = User.Create("test@example.com", "hash");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.UpdateGoals(new UserGoalUpdate(MondayCalories: mondayCalories)));
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

        user.UpdateDesiredWeight(desiredWeight: null);

        Assert.Null(user.DesiredWeight);
    }

    [Fact]
    public void UpdateDesiredWaist_WithNull_ClearsValue() {
        var user = User.Create("test@example.com", "hash");
        user.UpdateDesiredWaist(90);

        user.UpdateDesiredWaist(desiredWaist: null);

        Assert.Null(user.DesiredWaist);
    }

    [Fact]
    public void UpdateProfile_WithUnsupportedLanguage_Throws() {
        var user = User.Create("test@example.com", "hash");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.UpdatePreferences(new UserPreferenceUpdate(Language: "de")));
    }

    [Fact]
    public void UpdateProfile_WithSupportedLanguage_UpdatesValue() {
        var user = User.Create("test@example.com", "hash");

        user.UpdatePreferences(new UserPreferenceUpdate(Language: "ru"));

        Assert.Equal("ru", user.Language);
    }

    [Fact]
    public void UpdatePreferences_WithSupportedLanguage_UpdatesValue() {
        var user = User.Create("test@example.com", "hash");

        user.UpdatePreferences(new UserPreferenceUpdate(Language: "ru"));

        Assert.Equal("ru", user.Language);
    }

    [Fact]
    public void UpdatePreferences_WithUnsupportedTheme_Throws() {
        var user = User.Create("test@example.com", "hash");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.UpdatePreferences(new UserPreferenceUpdate(Theme: "sunset")));
    }

    [Fact]
    public void UpdatePreferences_WithSupportedTheme_UpdatesValue() {
        var user = User.Create("test@example.com", "hash");

        user.UpdatePreferences(new UserPreferenceUpdate(Theme: "leaf"));

        Assert.Equal("leaf", user.Theme);
    }

    [Fact]
    public void UpdatePreferences_WithUnsupportedUiStyle_Throws() {
        var user = User.Create("test@example.com", "hash");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.UpdatePreferences(new UserPreferenceUpdate(UiStyle: "retro")));
    }

    [Fact]
    public void UpdatePreferences_WithSupportedUiStyle_UpdatesValue() {
        var user = User.Create("test@example.com", "hash");

        user.UpdatePreferences(new UserPreferenceUpdate(UiStyle: "modern"));

        Assert.Equal("modern", user.UiStyle);
    }

    [Fact]
    public void UpdatePreferences_WithTypedUpdate_UpdatesDashboardLayoutAndLanguage() {
        var user = User.Create("test@example.com", "hash");

        user.UpdatePreferences(new UserPreferenceUpdate(
            DashboardLayoutJson: "{\"layout\":\"compact\"}",
            Language: "ru"));

        Assert.Equal("{\"layout\":\"compact\"}", user.DashboardLayoutJson);
        Assert.Equal("ru", user.Language);
    }

    [Fact]
    public void UpdatePreferences_WithNotificationAndReminderSettings_UpdatesState() {
        var user = User.Create("test@example.com", "hash");

        user.UpdatePreferences(new UserPreferenceUpdate(
            PushNotificationsEnabled: true,
            FastingPushNotificationsEnabled: true,
            SocialPushNotificationsEnabled: true,
            FastingCheckInReminderHours: 12,
            FastingCheckInFollowUpReminderHours: 36));

        Assert.Multiple(
            () => Assert.True(user.PushNotificationsEnabled),
            () => Assert.True(user.FastingPushNotificationsEnabled),
            () => Assert.True(user.SocialPushNotificationsEnabled),
            () => Assert.Equal(12, user.FastingCheckInReminderHours),
            () => Assert.Equal(36, user.FastingCheckInFollowUpReminderHours));
        Assert.NotNull(user.ModifiedOnUtc);
    }

    [Theory]
    [InlineData(0, 24)]
    [InlineData(169, 170)]
    [InlineData(24, 24)]
    public void UpdatePreferences_WithInvalidReminderHours_Throws(int firstReminder, int followUpReminder) {
        var user = User.Create("test@example.com", "hash");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.UpdatePreferences(new UserPreferenceUpdate(
                FastingCheckInReminderHours: firstReminder,
                FastingCheckInFollowUpReminderHours: followUpReminder)));
    }

    [Fact]
    public void UpdatePreferences_WithNoChanges_DoesNotSetModifiedOnUtc() {
        var user = User.Create("test@example.com", "hash");

        user.UpdatePreferences(new UserPreferenceUpdate());

        Assert.Null(user.ModifiedOnUtc);
    }

    [Fact]
    public void UpdatePreferences_WithWhitespaceDashboardLayout_NormalizesToEmptyString() {
        var user = User.Create("test@example.com", "hash");
        user.UpdatePreferences(new UserPreferenceUpdate(DashboardLayoutJson: "{\"layout\":\"compact\"}"));

        user.UpdatePreferences(new UserPreferenceUpdate(DashboardLayoutJson: "   "));

        Assert.Equal(string.Empty, user.DashboardLayoutJson);
        Assert.NotNull(user.ModifiedOnUtc);
    }

    [Fact]
    public void AcceptAiConsent_WhenNotAccepted_SetsTimestamp() {
        var user = User.Create("test@example.com", "hash");

        user.AcceptAiConsent();

        Assert.NotNull(user.AiConsentAcceptedAt);
        Assert.NotNull(user.ModifiedOnUtc);
    }

    [Fact]
    public void AcceptAiConsent_WhenAlreadyAccepted_DoesNotChangeTimestamp() {
        var user = User.Create("test@example.com", "hash");
        user.AcceptAiConsent();
        DateTime? acceptedAt = user.AiConsentAcceptedAt;
        DateTime? modifiedAt = user.ModifiedOnUtc;

        user.AcceptAiConsent();

        Assert.Equal(acceptedAt, user.AiConsentAcceptedAt);
        Assert.Equal(modifiedAt, user.ModifiedOnUtc);
    }

    [Fact]
    public void RevokeAiConsent_WhenNotAccepted_DoesNothing() {
        var user = User.Create("test@example.com", "hash");

        user.RevokeAiConsent();

        Assert.Null(user.AiConsentAcceptedAt);
        Assert.Null(user.ModifiedOnUtc);
    }

    [Fact]
    public void RevokeAiConsent_WhenAccepted_ClearsTimestamp() {
        var user = User.Create("test@example.com", "hash");
        user.AcceptAiConsent();

        user.RevokeAiConsent();

        Assert.Null(user.AiConsentAcceptedAt);
        Assert.NotNull(user.ModifiedOnUtc);
    }

    [Fact]
    public void User_Create_SetsDefaultTheme() {
        var user = User.Create("test@example.com", "hash");

        Assert.Equal("ocean", user.Theme);
    }

    [Fact]
    public void User_Create_SetsDefaultUiStyle() {
        var user = User.Create("test@example.com", "hash");

        Assert.Equal("classic", user.UiStyle);
    }

    [Fact]
    public void SetLanguage_WithSupportedLanguage_UpdatesValue() {
        var user = User.Create("test@example.com", "hash");

        user.SetLanguage("ru");

        Assert.Equal("ru", user.Language);
    }

    [Fact]
    public void UpdateAdminNarrowOperations_WithValidValues_UpdateAdminControlledFields() {
        var user = User.Create("test@example.com", "hash");

        user.UpdateAdminSecurity(new UserAdminSecurityUpdate(IsEmailConfirmed: true));
        user.UpdateAdminPreferences(new UserAdminPreferenceUpdate(Language: "ru"));
        user.UpdateAdminAiQuota(new UserAdminAiQuotaUpdate(
            AiInputTokenLimit: 123,
            AiOutputTokenLimit: 456));

        Assert.Multiple(
            () => Assert.True(user.IsEmailConfirmed),
            () => Assert.Equal("ru", user.Language),
            () => Assert.Equal(123, user.AiInputTokenLimit),
            () => Assert.Equal(456, user.AiOutputTokenLimit));
    }

    [Fact]
    public void UpdateAdminAiQuota_WithNegativeInputLimit_Throws() {
        var user = User.Create("test@example.com", "hash");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.UpdateAdminAiQuota(new UserAdminAiQuotaUpdate(AiInputTokenLimit: -1)));
    }

    [Fact]
    public void UpdateAdminAiQuota_WithNegativeOutputLimit_Throws() {
        var user = User.Create("test@example.com", "hash");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.UpdateAdminAiQuota(new UserAdminAiQuotaUpdate(AiOutputTokenLimit: -1)));
    }

    [Fact]
    public void UpdateAiTokenLimits_WithTypedUpdate_UpdatesChangedLimits() {
        var user = User.Create("test@example.com", "hash");

        user.UpdateAiTokenLimits(new UserAiTokenLimitUpdate(123, 456));

        Assert.Equal(123, user.AiInputTokenLimit);
        Assert.Equal(456, user.AiOutputTokenLimit);
        Assert.NotNull(user.ModifiedOnUtc);
    }

    [Fact]
    public void UpdateAiTokenLimits_WithTypedSameValues_DoesNotSetModifiedOnUtc() {
        var user = User.Create("test@example.com", "hash");

        user.UpdateAiTokenLimits(new UserAiTokenLimitUpdate(user.AiInputTokenLimit, user.AiOutputTokenLimit));

        Assert.Null(user.ModifiedOnUtc);
    }

    [Fact]
    public void UpdateAiTokenLimits_WithNullLimits_DoesNotSetModifiedOnUtc() {
        var user = User.Create("test@example.com", "hash");

        user.UpdateAiTokenLimits(inputLimit: null, outputLimit: null);

        Assert.Multiple(
            () => Assert.Equal(5_000_000, user.AiInputTokenLimit),
            () => Assert.Equal(1_000_000, user.AiOutputTokenLimit),
            () => Assert.Null(user.ModifiedOnUtc));
    }

    [Fact]
    public void AdminNarrowOperations_WithRolesAndAccountChanges_UpdateAdminControlledState() {
        var user = User.Create("test@example.com", "hash");
        var adminRole = Role.Create("Admin");
        var supportRole = Role.Create("Support");

        user.Deactivate();
        user.UpdateAdminSecurity(new UserAdminSecurityUpdate(IsEmailConfirmed: true));
        user.UpdateAdminPreferences(new UserAdminPreferenceUpdate(Language: "ru"));
        user.UpdateAdminAiQuota(new UserAdminAiQuotaUpdate(
            AiInputTokenLimit: 123,
            AiOutputTokenLimit: 456));
        user.ReplaceRoles([adminRole, supportRole]);

        Assert.Multiple(
            () => Assert.False(user.IsActive),
            () => Assert.True(user.IsEmailConfirmed),
            () => Assert.Equal("ru", user.Language),
            () => Assert.Equal(123, user.AiInputTokenLimit),
            () => Assert.Equal(456, user.AiOutputTokenLimit),
            () => Assert.Equal(["Admin", "Support"], [.. user.UserRoles.Select(role => role.Role.Name).Order(StringComparer.Ordinal)]));
    }

    [Fact]
    public void ReplaceRoles_WithSameRoles_DoesNotSetModifiedOnUtcAgain() {
        var user = User.Create("test@example.com", "hash");
        var adminRole = Role.Create("Admin");
        var supportRole = Role.Create("Support");
        user.ReplaceRoles([adminRole, supportRole]);
        DateTime? modifiedAt = user.ModifiedOnUtc;

        user.ReplaceRoles([supportRole, adminRole]);

        Assert.Equal(modifiedAt, user.ModifiedOnUtc);
        Assert.Equal(2, user.UserRoles.Count);
    }

    [Fact]
    public void UpdateProfile_WithUnsupportedGender_Throws() {
        var user = User.Create("test@example.com", "hash");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.UpdatePersonalInfo(new UserPersonalInfoUpdate(Gender: "X")));
    }

    [Fact]
    public void UpdateProfile_WithBlankGender_Throws() {
        var user = User.Create("test@example.com", "hash");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.UpdatePersonalInfo(new UserPersonalInfoUpdate(Gender: "   ")));
    }

    [Fact]
    public void UpdateProfile_WithSupportedGender_NormalizesToCanonicalCode() {
        var user = User.Create("test@example.com", "hash");

        user.UpdatePersonalInfo(new UserPersonalInfoUpdate(Gender: "f"));

        Assert.Equal("F", user.Gender);
    }

    [Fact]
    public void UpdatePersonalInfo_WithSupportedGender_NormalizesToCanonicalCode() {
        var user = User.Create("test@example.com", "hash");

        user.UpdatePersonalInfo(gender: "f");

        Assert.Equal("F", user.Gender);
    }

    [Fact]
    public void UpdatePersonalInfo_WithTypedUpdate_NormalizesGenderAndProfileText() {
        var user = User.Create("test@example.com", "hash");

        user.UpdatePersonalInfo(new UserPersonalInfoUpdate(
            Username: " alex ",
            FirstName: " Alexey ",
            Gender: "f"));

        Assert.Multiple(
            () => Assert.Equal("alex", user.Username),
            () => Assert.Equal("Alexey", user.FirstName),
            () => Assert.Equal("F", user.Gender));
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
    public void UpdateProfileMedia_WithTypedUpdate_UpdatesImageFields() {
        var user = User.Create("test@example.com", "hash");
        var assetId = FoodDiary.Domain.ValueObjects.Ids.ImageAssetId.New();

        user.UpdateProfileMedia(new UserProfileMediaUpdate(
            ProfileImage: " https://cdn.example.com/avatar-2.webp ",
            ProfileImageAssetId: assetId));

        Assert.Equal("https://cdn.example.com/avatar-2.webp", user.ProfileImage);
        Assert.Equal(assetId, user.ProfileImageAssetId);
    }

    [Fact]
    public void CalculateBmr_WhenRequiredProfileDataIsMissing_ReturnsNull() {
        var user = User.Create("test@example.com", "hash");

        Assert.Null(user.CalculateBmr());
        Assert.Null(user.CalculateEstimatedTdee());
    }

    [Fact]
    public void CalculateBmr_WithMaleProfile_UsesMifflinStJeorFormula() {
        var user = User.Create("test@example.com", "hash");
        DateTime birthDate = DateTime.UtcNow.Date.AddYears(-30);
        user.UpdatePersonalInfo(new UserPersonalInfoUpdate(
            BirthDate: birthDate,
            Gender: "M",
            Weight: 80,
            Height: 180));

        Assert.Equal(1780, user.CalculateBmr());
    }

    [Fact]
    public void CalculateBmr_WithFemaleProfile_UsesFemaleOffset() {
        var user = User.Create("test@example.com", "hash");
        DateTime birthDate = DateTime.UtcNow.Date.AddYears(-30);
        user.UpdatePersonalInfo(new UserPersonalInfoUpdate(
            BirthDate: birthDate,
            Gender: "F",
            Weight: 60,
            Height: 165));

        Assert.Equal(1320, user.CalculateBmr());
    }

    [Fact]
    public void CalculateEstimatedTdee_UsesExtremeActivityMultiplier() {
        var user = User.Create("test@example.com", "hash");
        DateTime birthDate = DateTime.UtcNow.Date.AddYears(-30);
        user.UpdatePersonalInfo(new UserPersonalInfoUpdate(
            BirthDate: birthDate,
            Gender: "M",
            Weight: 80,
            Height: 180));
        user.UpdateActivity(activityLevel: ActivityLevel.Extreme);

        Assert.Equal(3382, user.CalculateEstimatedTdee());
    }

    [Theory]
    [InlineData(ActivityLevel.Minimal, 2136)]
    [InlineData(ActivityLevel.Light, 2448)]
    [InlineData(ActivityLevel.Moderate, 2759)]
    [InlineData(ActivityLevel.High, 3070)]
    public void CalculateEstimatedTdee_CoversActivityMultipliers(ActivityLevel activityLevel, double expectedTdee) {
        var user = User.Create("test@example.com", "hash");
        DateTime birthDate = DateTime.UtcNow.Date.AddYears(-30);
        user.UpdatePersonalInfo(new UserPersonalInfoUpdate(
            BirthDate: birthDate,
            Gender: "M",
            Weight: 80,
            Height: 180));
        user.UpdateActivity(activityLevel: activityLevel);

        Assert.Equal(expectedTdee, user.CalculateEstimatedTdee());
    }

    [Fact]
    public void UpdateActivity_WithUnsupportedActivityLevel_Throws() {
        var user = User.Create("test@example.com", "hash");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.UpdateActivity(activityLevel: (ActivityLevel)999));
    }

    [Fact]
    public void GetActivityMultiplier_WithUnsupportedActivityLevel_Throws() {
        MethodInfo? method = typeof(User).GetMethod(
            "GetActivityMultiplier",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

        TargetInvocationException exception = Assert.Throws<System.Reflection.TargetInvocationException>(() =>
            method!.Invoke(null, [(ActivityLevel)999]));

        Assert.IsType<ArgumentOutOfRangeException>(exception.InnerException);
    }

    [Fact]
    public void CalculateBmr_WhenBirthdayHasNotHappenedThisYear_DecrementsAge() {
        var user = User.Create("test@example.com", "hash");
        DateTime birthDate = DateTime.UtcNow.Date.AddYears(-30).AddDays(1);
        user.UpdatePersonalInfo(new UserPersonalInfoUpdate(
            BirthDate: birthDate,
            Gender: "M",
            Weight: 80,
            Height: 180));

        Assert.Equal(1785, user.CalculateBmr());
    }

    [Fact]
    public void CalculateBmr_WhenFormulaIsNonPositive_ReturnsNull() {
        var user = User.Create("test@example.com", "hash");
        DateTime birthDate = DateTime.UtcNow.Date.AddYears(-120);
        user.UpdatePersonalInfo(new UserPersonalInfoUpdate(
            BirthDate: birthDate,
            Gender: "F",
            Weight: 1,
            Height: 1));

        Assert.Null(user.CalculateBmr());
    }

    [Fact]
    public void CalculateBmr_WhenAgeIsNotPositive_ReturnsNull() {
        var user = User.Create("test@example.com", "hash");
        user.UpdatePersonalInfo(new UserPersonalInfoUpdate(
            BirthDate: DateTime.UtcNow.Date,
            Gender: "M",
            Weight: 80,
            Height: 180));

        Assert.Null(user.CalculateBmr());
        Assert.Null(user.CalculateEstimatedTdee());
    }

    [Fact]
    public void UserRole_WithEmptyIds_Throws() {
        Assert.Throws<ArgumentException>(() =>
            new UserRole(FoodDiary.Domain.ValueObjects.Ids.UserId.Empty, FoodDiary.Domain.ValueObjects.Ids.RoleId.New()));
        Assert.Throws<ArgumentException>(() =>
            new UserRole(FoodDiary.Domain.ValueObjects.Ids.UserId.New(), FoodDiary.Domain.ValueObjects.Ids.RoleId.Empty));
    }

    [Fact]
    public void PremiumTrial_WhenNotStarted_HasNoActiveTrial() {
        var user = User.Create("test@example.com", "hash");

        Assert.False(user.HasUsedPremiumTrial());
        Assert.False(user.HasActivePremiumTrial(DateTime.UtcNow));
    }

    [Fact]
    public void StartPremiumTrial_WithValidValues_SetsTrialWindow() {
        var user = User.Create("test@example.com", "hash");
        DateTime startedAtLocal = DateTime.Now;

        user.StartPremiumTrial(startedAtLocal, TimeSpan.FromDays(7));

        Assert.Multiple(
            () => Assert.True(user.HasUsedPremiumTrial()),
            () => Assert.Equal(startedAtLocal.ToUniversalTime(), user.PremiumTrialStartedAtUtc),
            () => Assert.Equal(startedAtLocal.ToUniversalTime().AddDays(7), user.PremiumTrialEndsAtUtc),
            () => Assert.True(user.HasActivePremiumTrial(startedAtLocal.ToUniversalTime().AddDays(1))),
            () => Assert.False(user.HasActivePremiumTrial(startedAtLocal.ToUniversalTime().AddDays(8))));
        Assert.NotNull(user.ModifiedOnUtc);
    }

    [Fact]
    public void StartPremiumTrial_WithNonPositiveDuration_Throws() {
        var user = User.Create("test@example.com", "hash");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.StartPremiumTrial(DateTime.UtcNow, TimeSpan.Zero));
    }

    [Fact]
    public void StartPremiumTrial_WhenAlreadyUsed_Throws() {
        var user = User.Create("test@example.com", "hash");
        user.StartPremiumTrial(DateTime.UtcNow, TimeSpan.FromDays(7));

        Assert.Throws<InvalidOperationException>(() =>
            user.StartPremiumTrial(DateTime.UtcNow, TimeSpan.FromDays(7)));
    }

    [Fact]
    public void HasActivePremiumTrial_WithUnspecifiedTimestamp_Throws() {
        var user = User.Create("test@example.com", "hash");
        user.StartPremiumTrial(DateTime.UtcNow, TimeSpan.FromDays(7));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.HasActivePremiumTrial(DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)));
    }

    [Fact]
    public void MarkDeleted_SetsDeletedState_AndRaisesDomainEvent() {
        var user = User.Create("test@example.com", "hash");
        DateTime deletedAt = DateTime.UtcNow;

        user.MarkDeleted(deletedAt);

        Assert.False(user.IsActive);
        Assert.Equal(deletedAt, user.DeletedAt);
        Assert.Single(user.DomainEvents);
        Assert.IsType<UserDeletedDomainEvent>(user.DomainEvents[0]);
    }

    [Fact]
    public void MarkDeleted_WithLocalTimestamp_NormalizesToUtc() {
        var user = User.Create("test@example.com", "hash");
        DateTime deletedAtLocal = DateTime.Now;

        user.MarkDeleted(deletedAtLocal);

        Assert.Multiple(
            () => Assert.Equal(deletedAtLocal.ToUniversalTime(), user.DeletedAt),
            () => Assert.Equal(DateTimeKind.Utc, user.DeletedAt!.Value.Kind),
            () => Assert.Equal(deletedAtLocal.ToUniversalTime(), ((UserDeletedDomainEvent)user.DomainEvents[0]).DeletedAtUtc));
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
        int initialEventCount = user.DomainEvents.Count;

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
    public void DeleteAccount_ClearsRefreshTokenAndMarksDeleted() {
        var user = User.Create("test@example.com", "hash");
        user.UpdateRefreshToken("refresh-token", DateTime.UtcNow.AddMinutes(-5));
        user.SetEmailConfirmationToken("email-token", DateTime.UtcNow.AddMinutes(30));
        user.SetPasswordResetToken("reset-token", DateTime.UtcNow.AddMinutes(30));
        user.ClearDomainEvents();
        DateTime deletedAtUtc = DateTime.UtcNow;

        user.DeleteAccount(deletedAtUtc);

        Assert.Multiple(
            () => Assert.Null(user.RefreshToken),
            () => Assert.Null(user.EmailConfirmationTokenHash),
            () => Assert.Null(user.EmailConfirmationTokenExpiresAtUtc),
            () => Assert.Null(user.EmailConfirmationSentAtUtc),
            () => Assert.Null(user.PasswordResetTokenHash),
            () => Assert.Null(user.PasswordResetTokenExpiresAtUtc),
            () => Assert.Null(user.PasswordResetSentAtUtc),
            () => Assert.Equal(deletedAtUtc, user.DeletedAt),
            () => Assert.False(user.IsActive));
        Assert.Single(user.DomainEvents);
        Assert.IsType<UserDeletedDomainEvent>(user.DomainEvents[0]);
    }

    [Fact]
    public void MarkDeleted_ClearsOutstandingTransientTokens() {
        var user = User.Create("test@example.com", "hash");
        user.UpdateRefreshToken("refresh-token", DateTime.UtcNow.AddMinutes(-5));
        user.SetEmailConfirmationToken("email-token", DateTime.UtcNow.AddMinutes(30));
        user.SetPasswordResetToken("reset-token", DateTime.UtcNow.AddMinutes(30));

        user.MarkDeleted(DateTime.UtcNow);

        Assert.Multiple(
            () => Assert.Null(user.RefreshToken),
            () => Assert.Null(user.EmailConfirmationTokenHash),
            () => Assert.Null(user.EmailConfirmationTokenExpiresAtUtc),
            () => Assert.Null(user.EmailConfirmationSentAtUtc),
            () => Assert.Null(user.PasswordResetTokenHash),
            () => Assert.Null(user.PasswordResetTokenExpiresAtUtc),
            () => Assert.Null(user.PasswordResetSentAtUtc));
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
            user.UpdatePersonalInfo(new UserPersonalInfoUpdate(FirstName: "Alex")));
    }

    [Fact]
    public void SetPasswordResetToken_WhenDeleted_Throws() {
        var user = User.Create("test@example.com", "hash");
        user.MarkDeleted(DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            user.SetPasswordResetToken("reset-hash", DateTime.UtcNow.AddMinutes(30)));
    }

    private static UserRefreshTokenSession CreateRefreshTokenSession() =>
        UserRefreshTokenSession.Create(
            Guid.NewGuid(),
            UserId.New(),
            "refresh-hash",
            rememberMe: false,
            authProvider: "local",
            ipAddress: "127.0.0.1",
            userAgent: "test",
            DateTime.UtcNow);
}
