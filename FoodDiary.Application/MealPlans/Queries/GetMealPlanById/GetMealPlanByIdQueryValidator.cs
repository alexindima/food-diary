using FluentValidation;

namespace FoodDiary.Application.MealPlans.Queries.GetMealPlanById;

public class GetMealPlanByIdQueryValidator : AbstractValidator<GetMealPlanByIdQuery> {
    public GetMealPlanByIdQueryValidator() {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("User ID is required.");

        RuleFor(x => x.PlanId)
            .NotEmpty()
            .WithErrorCode("MealPlan.InvalidId")
            .WithMessage("Plan ID is required.");
    }
}
