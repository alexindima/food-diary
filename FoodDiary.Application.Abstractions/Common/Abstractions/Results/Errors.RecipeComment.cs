namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class RecipeComment {
        public static Error NotFound(Guid id) => new(
            "RecipeComment.NotFound",
            $"Comment with ID {id} was not found.",
            Kind: ErrorKind.NotFound);

        public static Error NotAuthor => new(
            "RecipeComment.NotAuthor",
            "You are not the author of this comment.",
            Kind: ErrorKind.Forbidden);
    }
}
