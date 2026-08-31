using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.RecipeComments.Common;

public static class RecipeCommentErrors {
    public static Error NotFound(Guid id) => new(
        "RecipeComment.NotFound",
        $"Comment with ID {id} was not found.",
        Kind: ErrorKind.NotFound);

    public static Error NotAuthor => new(
        "RecipeComment.NotAuthor",
        "You are not the author of this comment.",
        Kind: ErrorKind.Forbidden);
}
