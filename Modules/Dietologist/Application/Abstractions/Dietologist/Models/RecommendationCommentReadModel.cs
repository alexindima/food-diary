namespace FoodDiary.Application.Abstractions.Dietologist.Models;

public sealed record RecommendationCommentReadModel(
    Guid Id,
    Guid RecommendationId,
    Guid AuthorUserId,
    string? AuthorFirstName,
    string? AuthorLastName,
    string AuthorEmail,
    string Text,
    DateTime CreatedAtUtc);
