using FluentValidation;

namespace FoodDiary.Application.FavoriteMeals.Commands.AddFavoriteMeal;

public sealed class AddFavoriteMealCommandValidator : AbstractValidator<AddFavoriteMealCommand> {
    public AddFavoriteMealCommandValidator() {
        RuleFor(x => x.MealId)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Meal id must not be empty.");
    }
}
