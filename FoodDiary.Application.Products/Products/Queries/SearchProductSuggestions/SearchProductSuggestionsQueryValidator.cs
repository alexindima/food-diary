using FluentValidation;

namespace FoodDiary.Application.Products.Queries.SearchProductSuggestions;

public sealed class SearchProductSuggestionsQueryValidator : AbstractValidator<SearchProductSuggestionsQuery> {
    public SearchProductSuggestionsQueryValidator() {
        RuleFor(x => x.Search)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 20);
    }
}
