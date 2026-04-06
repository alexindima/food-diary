using FluentValidation;

namespace FoodDiary.Application.Usda.Queries.SearchUsdaFoods;

public sealed class SearchUsdaFoodsQueryValidator : AbstractValidator<SearchUsdaFoodsQuery> {
    public SearchUsdaFoodsQueryValidator() {
        RuleFor(x => x.Search)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Search query is required.");

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 100)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Limit must be between 1 and 100.");
    }
}
