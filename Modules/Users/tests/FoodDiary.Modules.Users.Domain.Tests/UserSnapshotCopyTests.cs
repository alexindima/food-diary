using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.ValueObjects;
using FoodDiary.Modules.Users.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class UserSnapshotCopyTests {
    [Fact]
    public void ProfileSnapshotCopy_ProjectsTheNewPersonalMediaAndPreferenceValuesIndependently() {
        var original = UserProfileState.CreateInitial();
        var birth = new DateTime(1992, 2, 29, 0, 0, 0, DateTimeKind.Utc);
        var image = ImageAssetId.New();
        UserProfileState copy = original with {
            Username = "snapshot",
            FirstName = "Anna",
            LastName = "Example",
            BirthDate = birth,
            Gender = "F",
            WeightKg = 68.25,
            HeightCm = 171.5,
            ActivityLevel = ActivityLevel.High,
            ProfileImage = "https://example.test/profile",
            ProfileImageAssetId = image,
            DashboardLayoutJson = "{\"web\":[\"water\"]}",
            Language = "ru",
            Theme = "dark",
            UiStyle = "modern",
            PushNotificationsEnabled = true,
            FastingPushNotificationsEnabled = false,
            SocialPushNotificationsEnabled = false,
            FastingCheckInReminderHours = 4,
            FastingCheckInFollowUpReminderHours = 9,
            SurfaceStyle = "matte",
        };

        Assert.Multiple(
            () => Assert.Equal(new UserPersonalProfileState("snapshot", "Anna", "Example", birth, "F", 68.25, 171.5, ActivityLevel.High), copy.PersonalInfo),
            () => Assert.Equal(new UserProfileMediaState("https://example.test/profile", image), copy.Media),
            () => Assert.Equal(new UserPreferenceState("{\"web\":[\"water\"]}", "ru", "dark", "modern", PushNotificationsEnabled: true,
                FastingPushNotificationsEnabled: false, SocialPushNotificationsEnabled: false,
                FastingReminderSchedule.FromStoredHours(4, 9), "matte"), copy.Preferences),
            () => Assert.Equal(UserProfileState.CreateInitial(), original));
    }

    [Fact]
    public void GoalSnapshotCopy_PreservesTheOriginalAndSuppliesDistinctWeekdayCalorieTargets() {
        var original = new UserGoalState(1800, 90, 60, 225, 25, 2000, 70, 80);
        UserGoalState copy = original with {
            DailyCalorieTarget = 2100,
            ProteinTarget = 110,
            FatTarget = 70,
            CarbTarget = 240,
            FiberTarget = 30,
            WaterGoal = 2200,
            CalorieCyclingEnabled = true,
            MondayCalories = 2001,
            TuesdayCalories = 2002,
            WednesdayCalories = 2003,
            ThursdayCalories = 2004,
            FridayCalories = 2005,
            SaturdayCalories = 2006,
            SundayCalories = 2007,
        };
        var schedule = new UserCalorieSchedule(copy.DailyCalorieTarget, copy.CalorieCyclingEnabled,
            copy.MondayCalories, copy.TuesdayCalories, copy.WednesdayCalories, copy.ThursdayCalories,
            copy.FridayCalories, copy.SaturdayCalories, copy.SundayCalories);

        Assert.Multiple(
            () => Assert.Equal(14028, schedule.GetWeeklyTarget()),
            () => Assert.Equal(new double?[] { 2001, 2002, 2003, 2004, 2005, 2006, 2007 },
                Enumerable.Range(0, 7).Select(day => schedule.GetTargetForDate(new DateTime(2026, 10, 19).AddDays(day)))),
            () => Assert.Equal(2100, copy.DailyCalorieTarget),
            () => Assert.Equal(110, copy.ProteinTarget),
            () => Assert.Equal(70, copy.FatTarget),
            () => Assert.Equal(240, copy.CarbTarget),
            () => Assert.Equal(30, copy.FiberTarget),
            () => Assert.Equal(2200, copy.WaterGoal),
            () => Assert.Equal(70, copy.DesiredWeightKg),
            () => Assert.Equal(80, copy.DesiredWaistCm),
            () => Assert.Equal(new UserGoalState(1800, 90, 60, 225, 25, 2000, 70, 80), original));
    }

    [Fact]
    public void IdentityCopies_KeepPublishedIdsStableWhenPreparingReplacementIdentities() {
        var value = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var replacement = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var user = new UserId(value);
        var role = new RoleId(value);
        var weight = new WeightGoalId(value);
        var waist = new WaistGoalId(value);

        Assert.Multiple(
            () => Assert.Equal(replacement, (user with { Value = replacement }).Value),
            () => Assert.Equal(replacement, (role with { Value = replacement }).Value),
            () => Assert.Equal(replacement, (weight with { Value = replacement }).Value),
            () => Assert.Equal(replacement, (waist with { Value = replacement }).Value),
            () => Assert.Equal(value, user.Value),
            () => Assert.Equal(value, role.Value),
            () => Assert.Equal(value, weight.Value),
            () => Assert.Equal(value, waist.Value));
    }
}
