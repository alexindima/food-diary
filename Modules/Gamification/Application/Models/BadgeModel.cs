namespace FoodDiary.Modules.Gamification.Application.Models;

public sealed record BadgeModel(
    string Key,
    string Category,
    int Threshold,
    bool IsEarned,
    string TitleRu = "",
    string TitleEn = "",
    string DescriptionRu = "",
    string DescriptionEn = "",
    string Icon = "trophy",
    DateTime? EarnedAtUtc = null);
