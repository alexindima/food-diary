using FluentValidation.TestHelper;
using FoodDiary.Application.FavoriteMeals.Commands.AddFavoriteMeal;

namespace FoodDiary.Application.Tests.FavoriteMeals;

[ExcludeFromCodeCoverage]
public class FavoriteMealsValidatorTests {
    private readonly AddFavoriteMealCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithEmptyMealId_HasError() {
        var command = new AddFavoriteMealCommand(Guid.NewGuid(), Guid.Empty, Name: null);
        TestValidationResult<AddFavoriteMealCommand> result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.MealId);
    }

    [Fact]
    public async Task Validate_WithValidCommand_NoErrors() {
        var command = new AddFavoriteMealCommand(Guid.NewGuid(), Guid.NewGuid(), "Breakfast");
        TestValidationResult<AddFavoriteMealCommand> result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
