using FluentValidation;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Recipes.Queries.GetRecipeById;

public class GetRecipeByIdQueryValidator : AbstractValidator<GetRecipeByIdQuery>
{
    public GetRecipeByIdQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .Must(id => id is not null && id.Value != UserId.Empty)
            .WithErrorCode("Authentication.InvalidToken");

        RuleFor(x => x.RecipeId)
            .Must(id => id != RecipeId.Empty)
            .WithErrorCode("Validation.Required")
            .WithMessage("RecipeId is required");
    }
}
