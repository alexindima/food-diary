namespace FoodDiary.Modules.Dietologist.Presentation.Responses;

public sealed record RecommendationTemplateHttpResponse(
    Guid Id,
    string Name,
    string Text,
    bool IsArchived,
    DateTime CreatedAtUtc,
    DateTime? ModifiedAtUtc);
