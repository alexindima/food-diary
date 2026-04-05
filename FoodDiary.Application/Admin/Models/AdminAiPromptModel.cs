namespace FoodDiary.Application.Admin.Models;

public sealed record AdminAiPromptModel(
    Guid Id,
    string Key,
    string Locale,
    string PromptText,
    int Version,
    bool IsActive,
    DateTime CreatedOnUtc,
    DateTime? UpdatedOnUtc);
