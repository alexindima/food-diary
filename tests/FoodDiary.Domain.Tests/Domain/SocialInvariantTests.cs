using FoodDiary.Domain.Entities.Social;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public class SocialInvariantTests {
    [Fact]
    public void RecipeLike_Create_WithEmptyUserId_Throws() {
        Assert.Throws<ArgumentException>(() =>
            RecipeLike.Create(UserId.Empty, RecipeId.New()));
    }

    [Fact]
    public void RecipeLike_Create_WithEmptyRecipeId_Throws() {
        Assert.Throws<ArgumentException>(() =>
            RecipeLike.Create(UserId.New(), RecipeId.Empty));
    }

    [Fact]
    public void RecipeLike_Create_WithValidIds_Succeeds() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();

        var like = RecipeLike.Create(userId, recipeId);

        Assert.Equal(userId, like.UserId);
        Assert.Equal(recipeId, like.RecipeId);
    }

}
