using FluentValidation;

namespace FoodDiary.Application.Recipes.Queries.GetRecipeById;

public class GetRecipeByIdQueryValidator : AbstractValidator<GetRecipeByIdQuery> {
    public GetRecipeByIdQueryValidator() {
        RuleFor(x => x.UserId)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .Must(id => id is not null && id.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken");

        RuleFor(x => x.RecipeId)
            .NotEqual(Guid.Empty)
            .WithErrorCode("Validation.Required")
            .WithMessage("RecipeId is required");
    }
}
