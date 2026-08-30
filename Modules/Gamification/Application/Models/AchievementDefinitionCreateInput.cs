namespace FoodDiary.Application.Gamification.Models;

public sealed record AchievementDefinitionCreateInput(
    string Key,
    string Category,
    string Metric,
    int Threshold,
    string TitleRu,
    string TitleEn,
    string DescriptionRu,
    string DescriptionEn,
    string Icon,
    int SortOrder,
    bool IsActive);
