using FoodDiary.Modules.Ai.Contracts.Models;

namespace FoodDiary.Modules.Ai.Application.Abstractions.Common;

public interface IAiPromptPreviewRenderer {
    string Render(AiPromptDraft draft);
    string GetResponseFormatJson(string key);
}
