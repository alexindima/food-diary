using FluentValidation.TestHelper;
using FoodDiary.Application.MealPlanning.MealPlans.Commands.AdoptMealPlan;
using FoodDiary.Application.MealPlanning.MealPlans.Commands.GenerateShoppingList;
using FoodDiary.Application.MealPlanning.MealPlans.Queries.GetMealPlanById;
using FoodDiary.Application.MealPlanning.MealPlans.Queries.GetMealPlans;

namespace FoodDiary.Application.Tests.MealPlans;

[ExcludeFromCodeCoverage]
public class MealPlansValidatorTests {
    [Fact]
    public async Task AdoptMealPlan_WithEmptyUserId_HasError() {
        var validator = new AdoptMealPlanCommandValidator();
        var command = new AdoptMealPlanCommand(UserId: null, Guid.NewGuid());
        TestValidationResult<AdoptMealPlanCommand> result = await validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task AdoptMealPlan_WithEmptyPlanId_HasError() {
        var validator = new AdoptMealPlanCommandValidator();
        var command = new AdoptMealPlanCommand(Guid.NewGuid(), Guid.Empty);
        TestValidationResult<AdoptMealPlanCommand> result = await validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.PlanId);
    }

    [Fact]
    public async Task AdoptMealPlan_WithValidCommand_NoErrors() {
        var validator = new AdoptMealPlanCommandValidator();
        var command = new AdoptMealPlanCommand(Guid.NewGuid(), Guid.NewGuid());
        TestValidationResult<AdoptMealPlanCommand> result = await validator.TestValidateAsync(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task GenerateShoppingList_WithEmptyPlanId_HasError() {
        var validator = new GenerateShoppingListCommandValidator();
        var command = new GenerateShoppingListCommand(Guid.NewGuid(), Guid.Empty);
        TestValidationResult<GenerateShoppingListCommand> result = await validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.PlanId);
    }

    [Fact]
    public async Task GetMealPlans_WithEmptyUserId_HasError() {
        var validator = new GetMealPlansQueryValidator();
        var query = new GetMealPlansQuery(UserId: null, DietType: null);
        TestValidationResult<GetMealPlansQuery> result = await validator.TestValidateAsync(query);

        result.ShouldHaveValidationErrorFor(q => q.UserId);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("1")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public async Task GetMealPlans_WithInvalidDietType_HasError(string dietType) {
        var validator = new GetMealPlansQueryValidator();

        TestValidationResult<GetMealPlansQuery> result = await validator.TestValidateAsync(
            new GetMealPlansQuery(Guid.NewGuid(), dietType));

        result.ShouldHaveValidationErrorFor(query => query.DietType);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("keto")]
    [InlineData("LowCarb")]
    public async Task GetMealPlans_WithSupportedDietType_HasNoDietTypeError(string? dietType) {
        var validator = new GetMealPlansQueryValidator();

        TestValidationResult<GetMealPlansQuery> result = await validator.TestValidateAsync(
            new GetMealPlansQuery(Guid.NewGuid(), dietType));

        result.ShouldNotHaveValidationErrorFor(query => query.DietType);
    }

    [Fact]
    public async Task GetMealPlanById_WithEmptyPlanId_HasError() {
        var validator = new GetMealPlanByIdQueryValidator();
        var query = new GetMealPlanByIdQuery(Guid.NewGuid(), Guid.Empty);
        TestValidationResult<GetMealPlanByIdQuery> result = await validator.TestValidateAsync(query);

        result.ShouldHaveValidationErrorFor(q => q.PlanId);
    }
}
