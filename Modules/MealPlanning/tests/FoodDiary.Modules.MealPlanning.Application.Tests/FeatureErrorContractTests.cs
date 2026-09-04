using FoodDiary.Application.Abstractions.MealPlans.Common;
using FoodDiary.Application.Abstractions.ShoppingLists.Common;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class FeatureErrorContractTests {
    [Fact]
    public void MealPlanErrors_HasOwnerAssemblyAndNamespace() {
        Assert.Multiple(
            () => Assert.Equal("FoodDiary.Modules.MealPlanning.Application.Abstractions", typeof(MealPlanErrors).Assembly.GetName().Name),
            () => Assert.Equal("FoodDiary.Application.Abstractions.MealPlans.Common", typeof(MealPlanErrors).Namespace));
    }

    [Fact]
    public void MealPlanErrors_PreservesEveryPublicErrorContract() {
        AssertError(MealPlanErrors.NotFound(Guid.Parse("12345678-1234-1234-1234-123456789abc")), "MealPlan.NotFound", "Meal plan with ID 12345678-1234-1234-1234-123456789abc was not found.", ErrorKind.NotFound);
        AssertError(MealPlanErrors.NotAccessible(Guid.Parse("12345678-1234-1234-1234-123456789abc")), "MealPlan.NotAccessible", "Meal plan with ID 12345678-1234-1234-1234-123456789abc was not found or is not accessible.", ErrorKind.NotFound);
        AssertError(MealPlanErrors.InvalidId, "MealPlan.InvalidId", "Meal plan ID is required.", ErrorKind.Validation);
        AssertError(MealPlanErrors.NotCurated, "MealPlan.NotCurated", "Only curated meal plans can be adopted.", ErrorKind.Validation);
    }

    [Fact]
    public void ShoppingListErrors_HasOwnerAssemblyAndNamespace() {
        Assert.Multiple(
            () => Assert.Equal("FoodDiary.Modules.MealPlanning.Application.Abstractions", typeof(ShoppingListErrors).Assembly.GetName().Name),
            () => Assert.Equal("FoodDiary.Application.Abstractions.ShoppingLists.Common", typeof(ShoppingListErrors).Namespace));
    }

    [Fact]
    public void ShoppingListErrors_PreservesEveryPublicErrorContract() {
        AssertError(ShoppingListErrors.NotFound(Guid.Parse("12345678-1234-1234-1234-123456789abc")), "ShoppingList.NotFound", "Shopping list with ID 12345678-1234-1234-1234-123456789abc was not found.", ErrorKind.NotFound);
        AssertError(ShoppingListErrors.CurrentNotFound(), "ShoppingList.NotFound", "Shopping list was not found.", ErrorKind.NotFound);
    }

    private static void AssertError(Error error, string code, string message, ErrorKind kind) {
        Assert.Multiple(
            () => Assert.Equal(code, error.Code),
            () => Assert.Equal(message, error.Message),
            () => Assert.Equal(kind, error.Kind),
            () => Assert.Null(error.Details));
    }
}
