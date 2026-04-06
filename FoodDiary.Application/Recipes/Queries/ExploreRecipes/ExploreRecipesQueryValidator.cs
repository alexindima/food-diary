using FluentValidation;

namespace FoodDiary.Application.Recipes.Queries.ExploreRecipes;

public sealed class ExploreRecipesQueryValidator : AbstractValidator<ExploreRecipesQuery> {
    private static readonly string[] ValidSortValues = ["newest", "popular"];

    public ExploreRecipesQueryValidator() {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Page must be at least 1.");

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 50)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Limit must be between 1 and 50.");

        RuleFor(x => x.SortBy)
            .Must(v => ValidSortValues.Contains(v, StringComparer.OrdinalIgnoreCase))
            .WithErrorCode("Validation.Invalid")
            .WithMessage("SortBy must be 'newest' or 'popular'.");
    }
}
