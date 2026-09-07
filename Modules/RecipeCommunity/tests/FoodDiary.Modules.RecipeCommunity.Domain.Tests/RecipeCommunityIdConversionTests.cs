using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class RecipeCommunityIdConversionTests {
    [Fact]
    public void RecipeCommentId_PreservesValueAcrossConversionsAndFormatting() {
        var value = Guid.Parse("12345678-1234-1234-1234-1234567890ab");
        var id = (RecipeCommentId)value;
        Guid roundTrip = id;

        Assert.Multiple(
            () => Assert.Equal(value, roundTrip),
            () => Assert.Equal(value, id.Value),
            () => Assert.Equal(value.ToString(), id.ToString()),
            () => Assert.Equal(Guid.Empty, RecipeCommentId.Empty.Value));
    }
    [Fact]
    public void RecipeLikeId_PreservesValueAcrossConversionsAndFormatting() {
        var value = Guid.Parse("12345678-1234-1234-1234-1234567890ab");
        var id = (RecipeLikeId)value;
        Guid roundTrip = id;

        Assert.Multiple(
            () => Assert.Equal(value, roundTrip),
            () => Assert.Equal(value, id.Value),
            () => Assert.Equal(value.ToString(), id.ToString()),
            () => Assert.Equal(Guid.Empty, RecipeLikeId.Empty.Value));
    }
}
