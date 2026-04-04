using FluentValidation;

namespace FoodDiary.Application.Consumptions.Commands.RepeatMeal;

public class RepeatMealCommandValidator : AbstractValidator<RepeatMealCommand> {
    public RepeatMealCommandValidator() {
        RuleFor(x => x.UserId)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .Must(id => id is not null && id.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken");

        RuleFor(x => x.MealId)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Meal ID is required");
    }
}
