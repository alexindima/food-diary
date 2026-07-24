namespace FoodDiary.Application.Dietologist.Models;

public sealed record RecommendationCommentModel(
    Guid Id,
    Guid RecommendationId,
    Guid AuthorUserId,
    string? AuthorFirstName,
    string? AuthorLastName,
    string AuthorEmail,
    string Text,
    DateTime CreatedAtUtc);
