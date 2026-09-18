using FoodDiary.Modules.Ai.Contracts.Models;

namespace FoodDiary.Modules.Ai.Application.Abstractions.Prompts;

public static class AiPromptDraftValidation {
    public static bool IsValid(AiPromptDraft draft, bool requireImage) =>
        draft.Locale is "en" or "ru" && AiPromptCatalog.IsValid(draft.Key, draft.PromptText)
        && draft.Text?.Length is not > 2048
        && (!string.Equals(draft.Key, "text-parse", StringComparison.Ordinal) || !string.IsNullOrWhiteSpace(draft.Text))
        && (!string.Equals(draft.Key, "vision", StringComparison.Ordinal) || !requireImage || (draft.ImageAssetId is { } id && id != Guid.Empty))
        && (!string.Equals(draft.Key, "nutrition", StringComparison.Ordinal) || (draft.Items is { Count: > 0 and <= 50 }
            && draft.Items.All(item => item is not null && !string.IsNullOrWhiteSpace(item.NameEn)
                && item.NameEn.Length <= 256 && !string.IsNullOrWhiteSpace(item.Unit)
                && item.Unit.Length <= 32 && item.Amount > 0 && item.Confidence >= 0)));
}
