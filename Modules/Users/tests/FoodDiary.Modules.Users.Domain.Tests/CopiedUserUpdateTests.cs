using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.ValueObjects;

namespace FoodDiary.Modules.Users.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class CopiedUserUpdateTests {
    private static readonly DateTime Now = new(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void GoalCopy_AppliesEveryTargetAndWeekdayWithoutChangingTheReusableTemplate() {
        var user = User.Create("goals-copy@example.test", "hash");
        UserGoalUpdate template = new();
        UserGoalUpdate update = template with {
            DailyCalorieTarget = 2100,
            ProteinTarget = 110,
            FatTarget = 70,
            CarbTarget = 240,
            FiberTarget = 30,
            WaterGoal = 2200,
            DesiredWeightKg = DesiredWeightKg.Create(68),
            DesiredWaistCm = DesiredWaistCm.Create(78),
            CalorieCyclingEnabled = true,
            MondayCalories = 2001,
            TuesdayCalories = 2002,
            WednesdayCalories = 2003,
            ThursdayCalories = 2004,
            FridayCalories = 2005,
            SaturdayCalories = 2006,
            SundayCalories = 2007,
        };

        user.UpdateGoals(update);

        Assert.Multiple(
            () => Assert.Equal(default, template),
            () => Assert.Equal(2100, user.DailyCalorieTarget),
            () => Assert.Equal(110, user.ProteinTarget),
            () => Assert.Equal(70, user.FatTarget),
            () => Assert.Equal(240, user.CarbTarget),
            () => Assert.Equal(30, user.FiberTarget),
            () => Assert.Equal(2200, user.WaterGoal),
            () => Assert.Equal(68, user.DesiredWeightKg),
            () => Assert.Equal(78, user.DesiredWaistCm),
            () => Assert.True(user.CalorieCyclingEnabled),
            () => Assert.Equal(new double?[] { 2001, 2002, 2003, 2004, 2005, 2006, 2007 },
                [user.MondayCalories, user.TuesdayCalories, user.WednesdayCalories, user.ThursdayCalories,
                    user.FridayCalories, user.SaturdayCalories, user.SundayCalories]));
    }

    [Fact]
    public void PersonalCopy_NormalizesTextAndPreservesTheBirthCalendarDateAndMeasurementUnits() {
        var user = User.Create("profile-copy@example.test", "hash");
        var birth = new DateTime(1992, 2, 29, 0, 0, 0, DateTimeKind.Utc);
        UserPersonalInfoUpdate template = new();

        user.UpdatePersonalInfo(template with {
            Username = " copy-user ",
            FirstName = " Anna ",
            LastName = " Example ",
            BirthDate = birth,
            BirthDateSpecified = true,
            Gender = " f ",
            WeightKg = ProfileWeightKg.Create(68.25),
            HeightCm = ProfileHeightCm.Create(171.5),
        });

        Assert.Multiple(
            () => Assert.Equal(default, template),
            () => Assert.Equal("copy-user", user.Username),
            () => Assert.Equal("Anna", user.FirstName),
            () => Assert.Equal("Example", user.LastName),
            () => Assert.Equal(birth, user.BirthDate),
            () => Assert.Equal("F", user.Gender),
            () => Assert.Equal(68.25, user.WeightKg),
            () => Assert.Equal(171.5, user.HeightCm));
        user.UpdatePersonalInfo(template with { BirthDateSpecified = true });
        Assert.Null(user.BirthDate);
        Assert.Equal(171.5, user.HeightCm);
    }

    [Fact]
    public void ActivityAndAdminCopies_ReachTheirOwnedStateWithoutChangingOtherPreferences() {
        var user = User.Create("admin-copy@example.test", "hash");
        UserActivityUpdate activity = new();
        UserAdminAiQuotaUpdate quota = new();
        UserAdminPreferenceUpdate preferences = new();
        UserAdminSecurityUpdate security = new();
        user.UpdateActivity(activity with { ActivityLevel = ActivityLevel.High, StepGoal = 8500, HydrationGoal = 2300 });
        user.UpdateAdminAiQuota(quota with { AiInputTokenLimit = 1200, AiOutputTokenLimit = 800 });
        user.UpdateAdminPreferences(preferences with { Language = "ru" });
        user.UpdateAdminSecurity(security with { IsEmailConfirmed = true });

        Assert.Multiple(
            () => Assert.Equal(ActivityLevel.High, user.ActivityLevel),
            () => Assert.Equal(8500, user.StepGoal),
            () => Assert.Equal(2300, user.HydrationGoal),
            () => Assert.Equal(1200, user.AiInputTokenLimit),
            () => Assert.Equal(800, user.AiOutputTokenLimit),
            () => Assert.Equal("ru", user.Language),
            () => Assert.True(user.IsEmailConfirmed),
            () => Assert.Equal(ThemeCode.Default.Value, user.Theme),
            () => Assert.Equal(default, activity),
            () => Assert.Equal(default, quota),
            () => Assert.Equal(default, preferences),
            () => Assert.Equal(default, security));
        UserAiTokenLimitUpdate limits = new();
        user.UpdateAiTokenLimits(limits with { InputLimit = 600, OutputLimit = 400 });
        Assert.Equal(600, user.AiInputTokenLimit);
        Assert.Equal(400, user.AiOutputTokenLimit);
        Assert.Equal(default, limits);
    }

    [Fact]
    public void PreferenceCopy_AppliesIndependentFlagsAndElapsedReminderDelays() {
        var user = User.Create("preferences-copy@example.test", "hash");
        UserPreferenceUpdate template = new();
        FastingReminderDelayUpdate delays = new();
        UserPreferenceUpdate update = template with {
            DashboardLayoutJson = "{\"web\":[\"water\"],\"mobile\":[\"meals\"]}",
            Language = "ru",
            Theme = "dark",
            UiStyle = "modern",
            SurfaceStyle = "matte",
            PushNotificationsEnabled = true,
            FastingPushNotificationsEnabled = false,
            SocialPushNotificationsEnabled = false,
            ReminderDelays = delays with { FirstHours = 4, FollowUpHours = 9 },
        };

        user.UpdatePreferences(update);

        Assert.Multiple(
            () => Assert.Equal(default, template),
            () => Assert.Equal(default, delays),
            () => Assert.Equal(update.DashboardLayoutJson, user.DashboardLayoutJson),
            () => Assert.Equal("ru", user.Language),
            () => Assert.Equal("dark", user.Theme),
            () => Assert.Equal("modern", user.UiStyle),
            () => Assert.Equal("matte", user.SurfaceStyle),
            () => Assert.True(user.PushNotificationsEnabled),
            () => Assert.False(user.FastingPushNotificationsEnabled),
            () => Assert.False(user.SocialPushNotificationsEnabled),
            () => Assert.Equal(4, update.FastingCheckInReminderHours),
            () => Assert.Equal(9, update.FastingCheckInFollowUpReminderHours),
            () => Assert.Equal(4, user.FastingCheckInReminderHours),
            () => Assert.Equal(9, user.FastingCheckInFollowUpReminderHours));
    }

    [Fact]
    public void ScalarPreferenceConstructor_PreservesPartialReminderInputsUntilTheOwnerMergesThem() {
        var user = User.Create("partial-preferences@example.test", "hash");
        var update = new UserPreferenceUpdate(language: "en", fastingCheckInFollowUpReminderHours: 24);

        user.UpdatePreferences(update);

        Assert.Multiple(
            () => Assert.Null(update.FastingCheckInReminderHours),
            () => Assert.Equal(24, update.FastingCheckInFollowUpReminderHours),
            () => Assert.Equal(12, user.FastingCheckInReminderHours),
            () => Assert.Equal(24, user.FastingCheckInFollowUpReminderHours),
            () => Assert.Equal("en", user.Language));
    }

    [Fact]
    public void MediaCopy_SelectsAnAssetAndExplicitlyClearsItWithoutAffectingPersonalInfo() {
        var user = User.Create("media-copy@example.test", "hash");
        var image = ImageAssetId.New();
        UserProfileMediaUpdate template = new();
        user.UpdatePersonalInfo(firstName: "Anna");
        user.UpdateProfileMedia(template with { ProfileImage = "https://example.test/photo", ProfileImageAssetId = image });
        Assert.Equal(image, user.ProfileImageAssetId);
        Assert.Equal("https://example.test/photo", user.ProfileImage);

        user.UpdateProfileMedia(template with { ProfileImageSpecified = true });

        Assert.Multiple(
            () => Assert.Null(user.ProfileImage),
            () => Assert.Null(user.ProfileImageAssetId),
            () => Assert.Equal("Anna", user.FirstName),
            () => Assert.Equal(default, template));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TokenIssueCopy_PreservesHashAndUtcIssuanceForBothTokenPurposes(bool passwordReset) {
        using IDisposable clock = DomainTime.Override(new FixedClock(Now));
        var user = User.Create("token-copy@example.test", "hash");
        var template = new UserTokenIssue("first-hash", Now.AddMinutes(10), Now);
        UserTokenIssue issue = template with { TokenHash = "second-hash", ExpiresAtUtc = Now.AddHours(1), IssuedAtUtc = Now.AddMinutes(-1) };

        if (passwordReset) { user.SetPasswordResetToken(issue); } else { user.SetEmailConfirmationToken(issue); }

        Assert.Multiple(
            () => Assert.Equal("second-hash", passwordReset ? user.PasswordResetTokenHash : user.EmailConfirmationTokenHash),
            () => Assert.Equal(Now.AddHours(1), passwordReset ? user.PasswordResetTokenExpiresAtUtc : user.EmailConfirmationTokenExpiresAtUtc),
            () => Assert.Equal(Now.AddMinutes(-1), passwordReset ? user.PasswordResetSentAtUtc : user.EmailConfirmationSentAtUtc),
            () => Assert.Equal("first-hash", template.TokenHash),
            () => Assert.Equal(Now, template.IssuedAtUtc),
            () => Assert.Equal(Now.AddMinutes(10), template.ExpiresAtUtc));
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedClock(DateTime now) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(now);
    }
}
