using FluentValidation;

namespace FoodDiary.Modules.MealPlanning.Application.MealPlans.Commands.DeleteMealPlan;

public sealed class DeleteMealPlanCommandValidator : AbstractValidator<DeleteMealPlanCommand> {
    public DeleteMealPlanCommandValidator() {
        RuleFor(command => command.UserId).NotEmpty()
            .WithErrorCode("Authentication.InvalidToken").WithMessage("User ID is required.");
        RuleFor(command => command.UserId).Must(id => id.HasValue && id.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken").WithMessage("User ID is required.");
        RuleFor(command => command.PlanId).NotEmpty()
            .WithErrorCode("MealPlan.InvalidId").WithMessage("Plan ID is required.");
    }
}
