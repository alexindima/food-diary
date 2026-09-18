using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Modules.Ai.Contracts.Models;
namespace FoodDiary.Modules.Admin.Application.Mappings;

internal static class AdminAiPromptDraftMappings {
    public static AiPromptDraft ToOwnerDraft(this AdminAiPromptDraft draft) => new(draft.Key, draft.Locale,
        draft.PromptText, draft.Text, draft.ImageAssetId, draft.FoodName is null ? null :
            [new FoodVisionItemModel(draft.FoodName, NameLocal: null, draft.Amount ?? 0, draft.Unit ?? "", Confidence: 1)]);
}
