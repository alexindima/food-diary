using FluentValidation;

namespace FoodDiary.Application.OpenFoodFacts.Queries.SearchProducts;

public sealed class SearchOpenFoodFactsQueryValidator : AbstractValidator<SearchOpenFoodFactsQuery> {
    public SearchOpenFoodFactsQueryValidator() {
        RuleFor(x => x.Search)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Search query is required.");

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 50)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Limit must be between 1 and 50.");
    }
}
