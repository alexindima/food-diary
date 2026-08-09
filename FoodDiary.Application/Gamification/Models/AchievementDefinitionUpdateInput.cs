namespace FoodDiary.Application.Gamification.Models;

public sealed record AchievementDefinitionUpdateInput(
    string Category,
    string Metric,
    int Threshold,
    string TitleRu,
    string TitleEn,
    string DescriptionRu,
    string DescriptionEn,
    string Icon,
    int SortOrder,
    bool IsActive,
    int Version);
