namespace FoodDiary.Modules.Admin.Presentation.Requests;

public sealed record CreateAdminAchievementDefinitionHttpRequest(
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
