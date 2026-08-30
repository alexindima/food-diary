using FluentValidation;

namespace FoodDiary.Application.OpenFoodFacts.Queries.SearchProducts;

public sealed class SearchOpenFoodFactsQueryValidator : AbstractValidator<SearchOpenFoodFactsQuery> {
    public const int MaximumSearchLength = 256;
    public const int MinimumLimit = 1;
    public const int MaximumLimit = 50;

    public SearchOpenFoodFactsQueryValidator() {
        RuleFor(x => x.Search)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Search query is required.")
            .MaximumLength(MaximumSearchLength)
            .WithErrorCode("Validation.Invalid")
            .WithMessage($"Search query must not exceed {MaximumSearchLength} characters.");

        RuleFor(x => x.Limit)
            .InclusiveBetween(MinimumLimit, MaximumLimit)
            .WithErrorCode("Validation.Invalid")
            .WithMessage($"Limit must be between {MinimumLimit} and {MaximumLimit}.");
    }
}
