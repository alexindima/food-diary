namespace FoodDiary.Modules.Admin.Presentation.Features.Admin.Requests;

public sealed record AdminAiPromptUpsertHttpRequest(
    string PromptText,
    bool IsActive);
