using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class RecipesIdConversionTests {
    [Fact]
    public void RecipeId_PreservesValueAcrossConversionsAndFormatting() {
        var value = Guid.Parse("12345678-1234-1234-1234-1234567890ab");
        var id = (RecipeId)value;
        Guid roundTrip = id;

        Assert.Multiple(
            () => Assert.Equal(value, roundTrip),
            () => Assert.Equal(value, id.Value),
            () => Assert.Equal(value.ToString(), id.ToString()),
            () => Assert.Equal(Guid.Empty, RecipeId.Empty.Value));
    }
    [Fact]
    public void RecipeIngredientId_PreservesValueAcrossConversionsAndFormatting() {
        var value = Guid.Parse("12345678-1234-1234-1234-1234567890ab");
        var id = (RecipeIngredientId)value;
        Guid roundTrip = id;

        Assert.Multiple(
            () => Assert.Equal(value, roundTrip),
            () => Assert.Equal(value, id.Value),
            () => Assert.Equal(value.ToString(), id.ToString()),
            () => Assert.Equal(Guid.Empty, RecipeIngredientId.Empty.Value));
    }
    [Fact]
    public void RecipeStepId_PreservesValueAcrossConversionsAndFormatting() {
        var value = Guid.Parse("12345678-1234-1234-1234-1234567890ab");
        var id = (RecipeStepId)value;
        Guid roundTrip = id;

        Assert.Multiple(
            () => Assert.Equal(value, roundTrip),
            () => Assert.Equal(value, id.Value),
            () => Assert.Equal(value.ToString(), id.ToString()),
            () => Assert.Equal(Guid.Empty, RecipeStepId.Empty.Value));
    }
}
