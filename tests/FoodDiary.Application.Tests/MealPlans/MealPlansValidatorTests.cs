using FluentValidation.TestHelper;
using FoodDiary.Application.MealPlans.Commands.AdoptMealPlan;
using FoodDiary.Application.MealPlans.Commands.GenerateShoppingList;
using FoodDiary.Application.MealPlans.Queries.GetMealPlanById;
using FoodDiary.Application.MealPlans.Queries.GetMealPlans;

namespace FoodDiary.Application.Tests.MealPlans;

public class MealPlansValidatorTests {
    [Fact]
    public async Task AdoptMealPlan_WithEmptyUserId_HasError() {
        var validator = new AdoptMealPlanCommandValidator();
        var command = new AdoptMealPlanCommand(null, Guid.NewGuid());
        var result = await validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task AdoptMealPlan_WithEmptyPlanId_HasError() {
        var validator = new AdoptMealPlanCommandValidator();
        var command = new AdoptMealPlanCommand(Guid.NewGuid(), Guid.Empty);
        var result = await validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.PlanId);
    }

    [Fact]
    public async Task AdoptMealPlan_WithValidCommand_NoErrors() {
        var validator = new AdoptMealPlanCommandValidator();
        var command = new AdoptMealPlanCommand(Guid.NewGuid(), Guid.NewGuid());
        var result = await validator.TestValidateAsync(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task GenerateShoppingList_WithEmptyPlanId_HasError() {
        var validator = new GenerateShoppingListCommandValidator();
        var command = new GenerateShoppingListCommand(Guid.NewGuid(), Guid.Empty);
        var result = await validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.PlanId);
    }

    [Fact]
    public async Task GetMealPlans_WithEmptyUserId_HasError() {
        var validator = new GetMealPlansQueryValidator();
        var query = new GetMealPlansQuery(null, null);
        var result = await validator.TestValidateAsync(query);

        result.ShouldHaveValidationErrorFor(q => q.UserId);
    }

    [Fact]
    public async Task GetMealPlanById_WithEmptyPlanId_HasError() {
        var validator = new GetMealPlanByIdQueryValidator();
        var query = new GetMealPlanByIdQuery(Guid.NewGuid(), Guid.Empty);
        var result = await validator.TestValidateAsync(query);

        result.ShouldHaveValidationErrorFor(q => q.PlanId);
    }
}
