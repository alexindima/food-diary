using FluentValidation;

namespace FoodDiary.Application.Recipes.Queries.GetRecipesOverview;

public sealed class GetRecipesOverviewQueryValidator : AbstractValidator<GetRecipesOverviewQuery> {
    public GetRecipesOverviewQueryValidator() {
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
