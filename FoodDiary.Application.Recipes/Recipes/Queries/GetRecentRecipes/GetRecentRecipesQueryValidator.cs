using FluentValidation;

namespace FoodDiary.Application.Recipes.Queries.GetRecentRecipes;

public sealed class GetRecentRecipesQueryValidator : AbstractValidator<GetRecentRecipesQuery> {
    public GetRecentRecipesQueryValidator() {
        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(id => id is not null && id.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user");
    }
}
