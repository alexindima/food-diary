namespace FoodDiary.Presentation.Api.Features.Admin.Responses;

public sealed record AdminAchievementDefinitionHttpResponse(
    Guid Id,
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
    bool IsActive,
    int Version);
