namespace FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;

public sealed record UserGoalVersion(long Revision, DateOnly EffectiveFrom, UserDailyGoals Goals);
