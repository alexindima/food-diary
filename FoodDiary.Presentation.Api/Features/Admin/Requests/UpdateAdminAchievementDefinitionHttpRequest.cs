namespace FoodDiary.Presentation.Api.Features.Admin.Requests;

public sealed record UpdateAdminAchievementDefinitionHttpRequest(
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
