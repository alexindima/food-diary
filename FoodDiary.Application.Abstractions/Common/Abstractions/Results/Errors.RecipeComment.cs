using FoodDiary.Application.Abstractions.RecipeComments.Common;

namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class RecipeComment {
        public static Error NotFound(Guid id) => RecipeCommentErrors.NotFound(id);

        public static Error NotAuthor => RecipeCommentErrors.NotAuthor;
    }
}
