namespace FoodDiary.Modules.Dietologist.Application.Abstractions.Models;

public sealed record RecommendationCommentReadModel(
    Guid Id,
    Guid RecommendationId,
    Guid AuthorUserId,
    string? AuthorFirstName,
    string? AuthorLastName,
    string? AuthorEmail,
    string Text,
    DateTime CreatedAtUtc);
