using FluentValidation;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Recipes.Queries.GetRecipes;

public class GetRecipesQueryValidator : AbstractValidator<GetRecipesQuery>
{
    public GetRecipesQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .Must(id => id is not null && id.Value != UserId.Empty)
            .WithErrorCode("Authentication.InvalidToken");

        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Page must be greater than zero");

        RuleFor(x => x.Limit)
            .GreaterThan(0)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Limit must be greater than zero");
    }
}
