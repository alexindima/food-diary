namespace FoodDiary.Presentation.Api.Features.Admin.Requests;

public sealed record AdminAiPromptUpsertHttpRequest(
    string PromptText,
    bool IsActive);
