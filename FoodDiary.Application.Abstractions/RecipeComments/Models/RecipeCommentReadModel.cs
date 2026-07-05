namespace FoodDiary.Application.Abstractions.RecipeComments.Models;

public sealed record RecipeCommentReadModel(
    Guid Id,
    Guid RecipeId,
    Guid UserId,
    string? AuthorUsername,
    string? AuthorFirstName,
    string Text,
    DateTime CreatedAtUtc,
    DateTime? ModifiedAtUtc);
