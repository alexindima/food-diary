namespace FoodDiary.Application.RecipeComments.Models;

public sealed record RecipeCommentModel(
    Guid Id,
    Guid RecipeId,
    Guid AuthorId,
    string? AuthorUsername,
    string? AuthorFirstName,
    string Text,
    DateTime CreatedAtUtc,
    DateTime? ModifiedAtUtc,
    bool IsOwnedByCurrentUser);
