using FluentValidation;

namespace FoodDiary.Application.MealPlanning.MealPlans.Queries.GetMealPlans;

public sealed class GetMealPlansQueryValidator : AbstractValidator<GetMealPlansQuery> {
    public GetMealPlansQueryValidator() {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("User ID is required.");
    }
}
