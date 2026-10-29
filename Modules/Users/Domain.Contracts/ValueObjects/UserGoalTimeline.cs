namespace FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;

public sealed record UserGoalTimeline(IReadOnlyList<UserGoalVersion> Versions) {
    // Revision order resolves multiple saves in one day, including a time-zone change.
    public UserDailyGoals? GetGoalsForDate(DateOnly date) => Versions
        .Where(version => version.EffectiveFrom <= date)
        .MaxBy(version => version.Revision)?.Goals;
}
