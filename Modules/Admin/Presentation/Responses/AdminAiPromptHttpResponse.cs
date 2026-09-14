namespace FoodDiary.Modules.Admin.Presentation.Responses;

public sealed record AdminAiPromptHttpResponse(
    Guid Id,
    string Key,
    string Locale,
    string PromptText,
    int Version,
    bool IsActive,
    DateTime CreatedOnUtc,
    DateTime? UpdatedOnUtc);
