namespace FoodDiary.Presentation.Api.Features.Dietologist.Responses;

public sealed record RecommendationTemplateHttpResponse(
    Guid Id,
    string Name,
    string Text,
    bool IsArchived,
    DateTime CreatedAtUtc,
    DateTime? ModifiedAtUtc);
