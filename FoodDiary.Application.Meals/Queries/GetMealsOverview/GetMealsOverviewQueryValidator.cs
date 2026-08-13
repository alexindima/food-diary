using FluentValidation;

namespace FoodDiary.Application.Meals.Queries.GetMealsOverview;

public sealed class GetMealsOverviewQueryValidator : AbstractValidator<GetMealsOverviewQuery> {
    public GetMealsOverviewQueryValidator() {
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
