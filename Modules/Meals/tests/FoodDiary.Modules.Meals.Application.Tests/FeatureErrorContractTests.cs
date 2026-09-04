using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class FeatureErrorContractTests {
    [Fact]
    public void MealErrors_HasOwnerAssemblyAndNamespace() {
        Assert.Multiple(
            () => Assert.Equal("FoodDiary.Modules.Meals.Application.Abstractions", typeof(MealErrors).Assembly.GetName().Name),
            () => Assert.Equal("FoodDiary.Application.Abstractions.Meals.Common", typeof(MealErrors).Namespace));
    }

    [Fact]
    public void MealErrors_PreservesEveryPublicErrorContract() {
        AssertError(MealErrors.NotFound(Guid.Parse("12345678-1234-1234-1234-123456789abc")), "Meal.NotFound", "Meal with ID 12345678-1234-1234-1234-123456789abc was not found.", ErrorKind.NotFound);
        AssertError(MealErrors.InvalidData("sample"), "Meal.InvalidData", "sample", ErrorKind.Internal);
    }

    private static void AssertError(Error error, string code, string message, ErrorKind kind) {
        Assert.Multiple(
            () => Assert.Equal(code, error.Code),
            () => Assert.Equal(message, error.Message),
            () => Assert.Equal(kind, error.Kind),
            () => Assert.Null(error.Details));
    }
}
