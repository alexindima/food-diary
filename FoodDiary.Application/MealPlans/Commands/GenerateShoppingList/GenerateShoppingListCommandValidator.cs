using FluentValidation;

namespace FoodDiary.Application.MealPlans.Commands.GenerateShoppingList;

public class GenerateShoppingListCommandValidator : AbstractValidator<GenerateShoppingListCommand> {
    public GenerateShoppingListCommandValidator() {
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
