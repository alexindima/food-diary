using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Recipes.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class RecipePublicIngredientTests {
    [Fact]
    public void PublicDescription_NormalizesValuesAndClearsTheUnitWhenTheNameIsRemoved() {
        RecipeIngredient ingredient = CreateIngredient();
        ingredient.SetPublicDescription("  Oats  ", "  grams  ");
        Assert.Multiple(() => Assert.Equal("Oats", ingredient.PublicName), () => Assert.Equal("grams", ingredient.PublicUnit));
        ingredient.SetPublicDescription("Oats", " ");
        Assert.Null(ingredient.PublicUnit);
        ingredient.SetPublicDescription(" ", "grams");
        Assert.Multiple(() => Assert.Null(ingredient.PublicName), () => Assert.Null(ingredient.PublicUnit));
    }

    [Theory]
    [InlineData(257, 1)]
    [InlineData(1, 129)]
    public void PublicDescription_RejectsOversizedValuesWithoutChangingTheSavedDescription(int nameLength, int unitLength) {
        RecipeIngredient ingredient = CreateIngredient();
        ingredient.SetPublicDescription("Oats", "grams");

        Assert.Throws<ArgumentException>(() => ingredient.SetPublicDescription(new string('n', nameLength), new string('u', unitLength)));
        Assert.Multiple(() => Assert.Equal("Oats", ingredient.PublicName), () => Assert.Equal("grams", ingredient.PublicUnit));
    }

    [Fact]
    public void ProductSnapshot_AcceptsTheLinkedProductAndCanBeCleared() {
        RecipeIngredient ingredient = CreateIngredient();
        var snapshot = new RecipeIngredientProductSnapshot(ingredient.ProductId!.Value, "Oats", MeasurementUnit.G,
            100, 389, 17, 7, 66, 10, 0, Visibility.Public);

        ingredient.SetProductSnapshot(snapshot);
        Assert.Same(snapshot, ingredient.ProductSnapshot);
        ingredient.SetProductSnapshot(snapshot: null);
        Assert.Multiple(() => Assert.Null(ingredient.ProductSnapshot), () => Assert.Equal(25, ingredient.Amount));
    }

    private static RecipeIngredient CreateIngredient() => Recipe.Create(UserId.New(), "Porridge", 1)
        .AddStep(1, "Cook").AddProductIngredient(ProductId.New(), ProductUnitQuantity.FromUnits(25));
}
