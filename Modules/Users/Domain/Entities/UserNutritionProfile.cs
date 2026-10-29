using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Domain.Entities;

public sealed class UserNutritionProfile : AggregateRoot<UserId> {
    private readonly List<UserGoalRevision> _goalHistory = [];
    public IReadOnlyCollection<UserGoalRevision> GoalHistory => _goalHistory.AsReadOnly();
    public long GoalRevision { get; private set; }
    public DateTime? BirthDate { get; internal set; }
    public string? Gender { get; internal set; }
    public double? WeightKg { get; internal set; }
    public double? DesiredWeightKg { get; internal set; }
    public double? DesiredWaistCm { get; internal set; }
    public double? HeightCm { get; internal set; }
    public ActivityLevel ActivityLevel { get; internal set; } = ActivityLevel.Moderate;
    public double? DailyCalorieTarget { get; internal set; }
    public double? ProteinTarget { get; internal set; }
    public double? FatTarget { get; internal set; }
    public double? CarbTarget { get; internal set; }
    public double? FiberTarget { get; internal set; }
    public int? StepGoal { get; internal set; }
    public double? WaterGoal { get; internal set; }
    public double? HydrationGoal { get; internal set; }
    public bool CalorieCyclingEnabled { get; internal set; }
    public double? MondayCalories { get; internal set; }
    public double? TuesdayCalories { get; internal set; }
    public double? WednesdayCalories { get; internal set; }
    public double? ThursdayCalories { get; internal set; }
    public double? FridayCalories { get; internal set; }
    public double? SaturdayCalories { get; internal set; }
    public double? SundayCalories { get; internal set; }

    private UserNutritionProfile() { }

    internal static UserNutritionProfile Create(UserId userId) {
        var state = new UserNutritionProfile { Id = userId };
        state.SetCreated();
        state._goalHistory.Add(UserGoalRevision.Capture(state, DateOnly.MinValue));
        return state;
    }

    public UserDailyGoals GetDailyGoals() => new(
        new UserCalorieSchedule(DailyCalorieTarget, CalorieCyclingEnabled, MondayCalories, TuesdayCalories,
            WednesdayCalories, ThursdayCalories, FridayCalories, SaturdayCalories, SundayCalories),
        ProteinTarget, FatTarget, CarbTarget, FiberTarget, WaterGoal, HydrationGoal, StepGoal);

    internal void RecordGoals(DateOnly effectiveFrom) {
        GoalRevision = checked(GoalRevision + 1);
        _goalHistory.Add(UserGoalRevision.Capture(this, effectiveFrom));
    }

    internal void Touch() => SetModified();
}
