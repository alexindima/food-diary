using FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Common;
using FoodDiary.Modules.MealPlanning.Application.MealPlans.Commands.SaveCatalogMealPlan;
using FoodDiary.Modules.MealPlanning.Domain.Entities.MealPlans;
using FoodDiary.Modules.MealPlanning.Domain.Enums;
using FoodDiary.Modules.MealPlanning.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.MealPlanning.Application.Tests.MealPlans;

[ExcludeFromCodeCoverage]
public sealed class SaveCatalogMealPlanTests {
    [Fact]
    public async Task ValidationFailures_UseCodesMappedToBadRequest() {
        var command = new SaveCatalogMealPlanCommand(Guid.Empty, string.Empty, new string('x', 2049), "Unknown", 32,
            double.NaN, IsPublished: true,
            [.. Enumerable.Range(0, 32).Select(_ => new CatalogDayInput(0,
                [.. Enumerable.Range(0, 21).Select(_ => new CatalogMealInput("Unknown", Guid.Empty, 101))]))]);

        FluentValidation.Results.ValidationResult result = await new SaveCatalogMealPlanCommandValidator().ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.All(result.Errors, error => Assert.StartsWith("Validation.", error.ErrorCode, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Publication_RequiresEveryDayFilled_ButDraftMayBeIncomplete(bool published, bool valid) {
        var command = new SaveCatalogMealPlanCommand(Id: null, "Plan", Description: null, "Balanced", 2, TargetCaloriesPerDay: null, published, [new CatalogDayInput(1, [])]);
        FluentValidation.Results.ValidationResult result = await new SaveCatalogMealPlanCommandValidator().ValidateAsync(command);
        Assert.Equal(valid, result.IsValid);
    }

    [Fact]
    public async Task Save_RejectsMissingOrPrivateRecipeBeforeMutatingTemplate() {
        IMealPlanCatalogRepository catalog = Substitute.For<IMealPlanCatalogRepository>();
        IMealPlanWriteRepository repository = Substitute.For<IMealPlanWriteRepository>();
        IMealPlanCatalogRecipeReader reader = Substitute.For<IMealPlanCatalogRecipeReader>();
        IMealPlanCompositionReader composition = Substitute.For<IMealPlanCompositionReader>();
        var plan = MealPlan.CreateCurated("Original", description: null, DietType.Balanced, 1, targetCaloriesPerDay: null);
        reader.GetPublicIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns(new HashSet<Guid>());
        var command = new SaveCatalogMealPlanCommand(plan.Id.Value, "Changed", Description: null, "Balanced", 1, TargetCaloriesPerDay: null, IsPublished: true,
            [new CatalogDayInput(1, [new CatalogMealInput("Lunch", Guid.NewGuid(), Servings: 1)])]);
        var handler = new SaveCatalogMealPlanCommandHandler(catalog, repository, reader, composition);

        FoodDiary.Results.Result<FoodDiary.Modules.MealPlanning.Application.MealPlans.Models.MealPlanModel> result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("MealPlan.RecipeUnavailable", result.Error.Code);
        Assert.Equal("Original", plan.Name);
        await catalog.DidNotReceive().GetForUpdateAsync(Arg.Any<MealPlanId>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0, "Lunch")]
    [InlineData(101, "Lunch")]
    [InlineData(1, "Unknown")]
    public async Task Save_RejectsInvalidMeal(int servings, string mealType) {
        var command = new SaveCatalogMealPlanCommand(Id: null, "Plan", Description: null, "Balanced", 1, TargetCaloriesPerDay: null, IsPublished: true,
            [new CatalogDayInput(1, [new CatalogMealInput(mealType, Guid.NewGuid(), servings)])]);
        Assert.False((await new SaveCatalogMealPlanCommandValidator().ValidateAsync(command)).IsValid);
    }
}
