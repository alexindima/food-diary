namespace FoodDiary.Modules.Ai.Contracts.Models;

public sealed record AiPromptDraft(string Key, string Locale, string PromptText,
    string? Text, Guid? ImageAssetId, IReadOnlyList<FoodVisionItemModel>? Items);
