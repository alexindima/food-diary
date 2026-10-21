using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Recipes.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class RecipesIdConversionTests {
    [Fact]
    public void RecipeId_PreservesValueAcrossConversionsAndFormatting() {
        var value = Guid.Parse("12345678-1234-1234-1234-1234567890ab");
        RecipeId original = RecipeId.Empty;
        RecipeId id = original with { Value = value };
        Guid roundTrip = id;

        Assert.Multiple(
            () => Assert.Equal(value, roundTrip),
            () => Assert.Equal(value, id.Value),
            () => Assert.Equal((RecipeId)value, id),
            () => Assert.Equal(Guid.Empty, original.Value),
            () => Assert.Equal(value.ToString(), id.ToString()),
            () => Assert.Equal(Guid.Empty, RecipeId.Empty.Value));
    }
    [Fact]
    public void RecipeIngredientId_PreservesValueAcrossConversionsAndFormatting() {
        var value = Guid.Parse("12345678-1234-1234-1234-1234567890ab");
        RecipeIngredientId original = RecipeIngredientId.Empty;
        RecipeIngredientId id = original with { Value = value };
        Guid roundTrip = id;

        Assert.Multiple(
            () => Assert.Equal(value, roundTrip),
            () => Assert.Equal(value, id.Value),
            () => Assert.Equal((RecipeIngredientId)value, id),
            () => Assert.Equal(Guid.Empty, original.Value),
            () => Assert.Equal(value.ToString(), id.ToString()),
            () => Assert.Equal(Guid.Empty, RecipeIngredientId.Empty.Value));
    }
    [Fact]
    public void RecipeStepId_PreservesValueAcrossConversionsAndFormatting() {
        var value = Guid.Parse("12345678-1234-1234-1234-1234567890ab");
        RecipeStepId original = RecipeStepId.Empty;
        RecipeStepId id = original with { Value = value };
        Guid roundTrip = id;

        Assert.Multiple(
            () => Assert.Equal(value, roundTrip),
            () => Assert.Equal(value, id.Value),
            () => Assert.Equal((RecipeStepId)value, id),
            () => Assert.Equal(Guid.Empty, original.Value),
            () => Assert.Equal(value.ToString(), id.ToString()),
            () => Assert.Equal(Guid.Empty, RecipeStepId.Empty.Value));
    }
}
