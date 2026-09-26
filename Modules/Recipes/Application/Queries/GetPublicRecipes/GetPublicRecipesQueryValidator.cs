using FluentValidation;
using FoodDiary.Application.Abstractions.Common.Validation;

namespace FoodDiary.Modules.Recipes.Application.Queries.GetPublicRecipes;

public sealed class GetPublicRecipesQueryValidator : AbstractValidator<GetPublicRecipesQuery> {
    public GetPublicRecipesQueryValidator() {
        RuleFor(query => query.Page).InclusiveBetween(1, PaginationPolicy.MaxPageNumber).WithErrorCode("Validation.Invalid");
        RuleFor(query => query.Limit).InclusiveBetween(1, 50).WithErrorCode("Validation.Invalid");
        RuleFor(query => query.MaxTotalTime).GreaterThan(0).WithErrorCode("Validation.Invalid").When(query => query.MaxTotalTime.HasValue);
        RuleFor(query => query.Search).MaximumLength(128).WithErrorCode("Validation.Invalid");
        RuleFor(query => query.Category).MaximumLength(64).WithErrorCode("Validation.Invalid");
    }
}
