using FluentValidation.TestHelper;
using FoodDiary.Application.FavoriteMeals.Commands.AddFavoriteMeal;

namespace FoodDiary.Application.Tests.FavoriteMeals;

public class FavoriteMealsValidatorTests {
    private readonly AddFavoriteMealCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithEmptyMealId_HasError() {
        var command = new AddFavoriteMealCommand(Guid.NewGuid(), Guid.Empty, null);
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.MealId);
    }

    [Fact]
    public async Task Validate_WithValidCommand_NoErrors() {
        var command = new AddFavoriteMealCommand(Guid.NewGuid(), Guid.NewGuid(), "Breakfast");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
