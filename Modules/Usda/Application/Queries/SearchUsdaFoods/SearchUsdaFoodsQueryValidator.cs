using FluentValidation;

namespace FoodDiary.Application.Usda.Queries.SearchUsdaFoods;

public sealed class SearchUsdaFoodsQueryValidator : AbstractValidator<SearchUsdaFoodsQuery> {
    public const int MaximumSearchLength = 256;
    public const int MinimumLimit = 1;
    public const int MaximumLimit = 100;

    public SearchUsdaFoodsQueryValidator() {
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
