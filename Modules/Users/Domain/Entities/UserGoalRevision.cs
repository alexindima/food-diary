using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Domain.Entities;

public sealed class UserGoalRevision {
    public UserGoalRevisionId Id { get; private set; }
    public UserId UserId { get; private set; }
    public long Revision { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateTime RecordedAtUtc { get; private set; }
    public double? DailyCalorieTarget { get; private set; }
    public double? ProteinTarget { get; private set; }
    public double? FatTarget { get; private set; }
    public double? CarbTarget { get; private set; }
    public double? FiberTarget { get; private set; }
    public double? WaterGoal { get; private set; }
    public double? HydrationGoal { get; private set; }
    public int? StepGoal { get; private set; }
    public bool CalorieCyclingEnabled { get; private set; }
    public double? MondayCalories { get; private set; }
    public double? TuesdayCalories { get; private set; }
    public double? WednesdayCalories { get; private set; }
    public double? ThursdayCalories { get; private set; }
    public double? FridayCalories { get; private set; }
    public double? SaturdayCalories { get; private set; }
    public double? SundayCalories { get; private set; }

    private UserGoalRevision() { }

    internal static UserGoalRevision Capture(UserNutritionProfile profile, DateOnly effectiveFrom) => new() {
        Id = UserGoalRevisionId.New(),
        UserId = profile.Id,
        Revision = profile.GoalRevision,
        EffectiveFrom = effectiveFrom,
        RecordedAtUtc = DomainTime.UtcNow,
        DailyCalorieTarget = profile.DailyCalorieTarget,
        ProteinTarget = profile.ProteinTarget,
        FatTarget = profile.FatTarget,
        CarbTarget = profile.CarbTarget,
        FiberTarget = profile.FiberTarget,
        WaterGoal = profile.WaterGoal,
        HydrationGoal = profile.HydrationGoal,
        StepGoal = profile.StepGoal,
        CalorieCyclingEnabled = profile.CalorieCyclingEnabled,
        MondayCalories = profile.MondayCalories,
        TuesdayCalories = profile.TuesdayCalories,
        WednesdayCalories = profile.WednesdayCalories,
        ThursdayCalories = profile.ThursdayCalories,
        FridayCalories = profile.FridayCalories,
        SaturdayCalories = profile.SaturdayCalories,
        SundayCalories = profile.SundayCalories,
    };

    public UserDailyGoals ToDailyGoals() => new(
        new UserCalorieSchedule(DailyCalorieTarget, CalorieCyclingEnabled, MondayCalories, TuesdayCalories,
            WednesdayCalories, ThursdayCalories, FridayCalories, SaturdayCalories, SundayCalories),
        ProteinTarget, FatTarget, CarbTarget, FiberTarget, WaterGoal, HydrationGoal, StepGoal);
}
