namespace FoodDiary.Modules.Admin.Application.Models;

public sealed record AdminAiPromptModel(
    Guid Id,
    string Key,
    string Locale,
    string PromptText,
    int Version,
    bool IsActive,
    DateTime CreatedOnUtc,
    DateTime? UpdatedOnUtc);
