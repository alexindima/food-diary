namespace FoodDiary.Modules.Admin.Application.Models;

public sealed record AdminAiPromptDraft(string Key, string Locale, string PromptText,
    string? Text, Guid? ImageAssetId, string? FoodName, decimal? Amount, string? Unit);
