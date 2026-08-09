namespace FoodDiary.Application.Abstractions.Achievements.Models;

public sealed record AchievementGrantModel(
    string AchievementKey,
    DateTime EarnedAtUtc,
    int EarnedValue,
    int DefinitionVersion);
