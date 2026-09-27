using FoodDiary.Modules.Recipes.Domain.Contracts.Enums;
using FluentValidation;
using FoodDiary.Application.Contracts.Common.Validation;

namespace FoodDiary.Modules.Recipes.Application.Queries.GetPublicRecipes;

public sealed class GetPublicRecipesQueryValidator : AbstractValidator<GetPublicRecipesQuery> {
    public GetPublicRecipesQueryValidator() {
        RuleFor(query => query.Language).Must(value => value is null or "en" or "ru").WithErrorCode("Validation.Invalid");
        RuleFor(query => query.SortBy).NotNull().Must(value => value is "newest" or "fastest" or "name").WithErrorCode("Validation.Invalid");
        RuleFor(query => query.Page).InclusiveBetween(1, PaginationPolicy.MaxPageNumber).WithErrorCode("Validation.Invalid");
        RuleFor(query => query.Limit).InclusiveBetween(1, 50).WithErrorCode("Validation.Invalid");
        RuleFor(query => query.MaxTotalTime).GreaterThan(0).WithErrorCode("Validation.Invalid").When(query => query.MaxTotalTime.HasValue);
        RuleFor(query => query.Search).MaximumLength(128).WithErrorCode("Validation.Invalid");
        RuleFor(query => query.Category).Must(code => code is null || RecipeCategoryCodes.IsValid(code)).WithErrorCode("Validation.Invalid");
    }
}
