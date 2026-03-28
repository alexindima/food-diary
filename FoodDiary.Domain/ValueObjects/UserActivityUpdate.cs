using FoodDiary.Domain.Enums;

namespace FoodDiary.Domain.ValueObjects;

public readonly record struct UserActivityUpdate(
    ActivityLevel? ActivityLevel = null,
    int? StepGoal = null,
    double? HydrationGoal = null);
