using FluentValidation;

namespace FoodDiary.Application.MealPlans.Commands.AdoptMealPlan;

public class AdoptMealPlanCommandValidator : AbstractValidator<AdoptMealPlanCommand> {
    public AdoptMealPlanCommandValidator() {
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
