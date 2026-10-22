using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Recipes.Domain.Contracts.Enums;
using FoodDiary.Modules.Recipes.Domain.ValueObjects;

namespace FoodDiary.Modules.Recipes.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class RecipeStateCopyContractTests {
    [Theory]
    [InlineData(Visibility.Public, Visibility.Private)]
    [InlineData(Visibility.Private, Visibility.Public)]
    public void ChangingSnapshotVisibility_PreservesRecipeDetailsAndTheOriginal(Visibility originalVisibility, Visibility visibility) {
        RecipeDetailsState original = default(RecipeDetailsState) with {
            Name = "Vegetable soup",
            Description = "Three portions",
            Comment = "Keep chilled",
            Category = RecipeCategory.Soups,
            ImageUrl = "https://images.example.test/soup.png",
            PrepTime = 12,
            CookTime = 27,
            Servings = 3,
            Visibility = originalVisibility,
        };

        RecipeDetailsState copy = original with { Visibility = visibility };

        Assert.Multiple(
            () => Assert.Equal(originalVisibility, original.Visibility),
            () => Assert.Equal(visibility, copy.Visibility),
            () => Assert.Equal("Vegetable soup", copy.Name),
            () => Assert.Equal("Three portions", copy.Description),
            () => Assert.Equal("Keep chilled", copy.Comment),
            () => Assert.Equal(RecipeCategory.Soups, copy.Category),
            () => Assert.Equal("https://images.example.test/soup.png", copy.ImageUrl),
            () => Assert.Null(copy.ImageAssetId),
            () => Assert.Equal(12, copy.PrepTime),
            () => Assert.Equal(27, copy.CookTime),
            () => Assert.Equal(3, copy.Servings));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ChangingSnapshotNutritionMode_PreservesFractionalTotalsManualValuesAndAbsence(bool automatic, bool changedMode) {
        var original = new RecipeNutritionState(
            TotalCalories: 333.33, TotalProteins: 12.125, TotalFats: 8.75, TotalCarbs: 40.625,
            TotalFiber: null, TotalAlcohol: 0, IsNutritionAutoCalculated: automatic,
            ManualCalories: 250.125, ManualProteins: null, ManualFats: 6.25, ManualCarbs: 30.375,
            ManualFiber: 2.75, ManualAlcohol: null);

        RecipeNutritionState copy = original with { IsNutritionAutoCalculated = changedMode };

        Assert.Multiple(
            () => Assert.Equal(automatic, original.IsNutritionAutoCalculated),
            () => Assert.Equal(changedMode, copy.IsNutritionAutoCalculated),
            () => Assert.Equal(333.33, copy.TotalCalories),
            () => Assert.Equal(12.125, copy.TotalProteins),
            () => Assert.Equal(8.75, copy.TotalFats),
            () => Assert.Equal(40.625, copy.TotalCarbs),
            () => Assert.Null(copy.TotalFiber),
            () => Assert.Equal(0, copy.TotalAlcohol),
            () => Assert.Equal(250.125, copy.ManualCalories),
            () => Assert.Null(copy.ManualProteins),
            () => Assert.Equal(6.25, copy.ManualFats),
            () => Assert.Equal(30.375, copy.ManualCarbs),
            () => Assert.Equal(2.75, copy.ManualFiber),
            () => Assert.Null(copy.ManualAlcohol));
    }
}
