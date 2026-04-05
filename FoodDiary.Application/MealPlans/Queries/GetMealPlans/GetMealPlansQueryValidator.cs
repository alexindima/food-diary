using FluentValidation;

namespace FoodDiary.Application.MealPlans.Queries.GetMealPlans;

public class GetMealPlansQueryValidator : AbstractValidator<GetMealPlansQuery> {
    public GetMealPlansQueryValidator() {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("User ID is required.");
    }
}
