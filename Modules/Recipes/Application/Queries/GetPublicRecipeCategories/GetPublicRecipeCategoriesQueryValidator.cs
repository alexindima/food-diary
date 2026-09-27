using FluentValidation;

namespace FoodDiary.Modules.Recipes.Application.Queries.GetPublicRecipeCategories;

public sealed class GetPublicRecipeCategoriesQueryValidator : AbstractValidator<GetPublicRecipeCategoriesQuery> {
    public GetPublicRecipeCategoriesQueryValidator() {
        RuleFor(query => query.Search).MaximumLength(64).WithErrorCode("Validation.Invalid");
        RuleFor(query => query.Language).Must(value => value is null or "en" or "ru").WithErrorCode("Validation.Invalid");
    }
}
