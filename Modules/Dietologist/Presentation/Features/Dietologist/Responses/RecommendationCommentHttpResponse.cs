namespace FoodDiary.Presentation.Api.Features.Dietologist.Responses;

public sealed record RecommendationCommentHttpResponse(
    Guid Id,
    Guid RecommendationId,
    Guid AuthorUserId,
    string? AuthorFirstName,
    string? AuthorLastName,
    string AuthorEmail,
    string Text,
    DateTime CreatedAtUtc);
