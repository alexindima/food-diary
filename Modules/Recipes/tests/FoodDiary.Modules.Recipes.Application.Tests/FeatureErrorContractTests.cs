using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class FeatureErrorContractTests {
    [Fact]
    public void RecipeErrors_HasOwnerAssemblyAndNamespace() {
        Assert.Multiple(
            () => Assert.Equal("FoodDiary.Modules.Recipes.Application.Abstractions", typeof(RecipeErrors).Assembly.GetName().Name),
            () => Assert.Equal("FoodDiary.Application.Abstractions.Recipes.Common", typeof(RecipeErrors).Namespace));
    }

    [Fact]
    public void RecipeErrors_PreservesEveryPublicErrorContract() {
        AssertError(RecipeErrors.NotFound(Guid.Parse("12345678-1234-1234-1234-123456789abc")), "Recipe.NotFound", "Recipe with ID 12345678-1234-1234-1234-123456789abc was not found.", ErrorKind.NotFound);
        AssertError(RecipeErrors.NotAccessible(Guid.Parse("12345678-1234-1234-1234-123456789abc")), "Recipe.NotAccessible", "Recipe with ID 12345678-1234-1234-1234-123456789abc does not belong to the current user or was not found.", ErrorKind.NotFound);
        AssertError(RecipeErrors.InvalidData("sample"), "Recipe.InvalidData", "sample", ErrorKind.Internal);
    }

    private static void AssertError(Error error, string code, string message, ErrorKind kind) {
        Assert.Multiple(
            () => Assert.Equal(code, error.Code),
            () => Assert.Equal(message, error.Message),
            () => Assert.Equal(kind, error.Kind),
            () => Assert.Null(error.Details));
    }
}
