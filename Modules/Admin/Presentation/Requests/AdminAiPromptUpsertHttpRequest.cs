namespace FoodDiary.Modules.Admin.Presentation.Requests;

public sealed record AdminAiPromptUpsertHttpRequest(
    string PromptText,
    bool IsActive);
