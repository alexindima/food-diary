namespace FoodDiary.Modules.Dietologist.Application.Models;

public sealed record RecommendationCommentModel(
    Guid Id,
    Guid RecommendationId,
    Guid AuthorUserId,
    string? AuthorFirstName,
    string? AuthorLastName,
    string? AuthorEmail,
    string Text,
    DateTime CreatedAtUtc);
