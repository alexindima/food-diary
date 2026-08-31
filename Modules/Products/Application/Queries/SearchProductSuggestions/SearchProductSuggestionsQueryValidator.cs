using FluentValidation;

namespace FoodDiary.Application.Products.Queries.SearchProductSuggestions;

public sealed class SearchProductSuggestionsQueryValidator : AbstractValidator<SearchProductSuggestionsQuery> {
    public const int MaximumSearchLength = 100;
    public const int MinimumLimit = 1;
    public const int MaximumLimit = 20;

    public SearchProductSuggestionsQueryValidator() {
        RuleFor(x => x.Search)
            .NotEmpty()
            .MaximumLength(MaximumSearchLength);

        RuleFor(x => x.Limit)
            .InclusiveBetween(MinimumLimit, MaximumLimit);
    }
}
