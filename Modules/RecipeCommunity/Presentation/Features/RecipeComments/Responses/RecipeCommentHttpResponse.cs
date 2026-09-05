namespace FoodDiary.Presentation.Api.Features.RecipeComments.Responses;

public sealed record RecipeCommentHttpResponse(
    Guid Id,
    Guid RecipeId,
    Guid AuthorId,
    string? AuthorUsername,
    string? AuthorFirstName,
    string Text,
    DateTime CreatedAtUtc,
    DateTime? ModifiedAtUtc,
    bool IsOwnedByCurrentUser);
