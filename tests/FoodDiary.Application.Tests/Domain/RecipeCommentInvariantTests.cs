using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Domain;

public class RecipeCommentInvariantTests {
    [Fact]
    public void Create_WithEmptyUserId_Throws() {
        Assert.Throws<ArgumentException>(() =>
            RecipeComment.Create(UserId.Empty, RecipeId.New(), "Great recipe!"));
    }

    [Fact]
    public void Create_WithEmptyRecipeId_Throws() {
        Assert.Throws<ArgumentException>(() =>
            RecipeComment.Create(UserId.New(), RecipeId.Empty, "Great recipe!"));
    }

    [Fact]
    public void Create_WithBlankText_Throws() {
        Assert.Throws<ArgumentException>(() =>
            RecipeComment.Create(UserId.New(), RecipeId.New(), "   "));
    }

    [Fact]
    public void Create_WithTooLongText_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RecipeComment.Create(UserId.New(), RecipeId.New(), new string('a', 2001)));
    }

    [Fact]
    public void Create_TrimsText() {
        var comment = RecipeComment.Create(UserId.New(), RecipeId.New(), "  Great recipe!  ");

        Assert.Equal("Great recipe!", comment.Text);
    }

    [Fact]
    public void UpdateText_WithNewValue_UpdatesText() {
        var comment = RecipeComment.Create(UserId.New(), RecipeId.New(), "Good");

        comment.UpdateText("  Excellent!  ");

        Assert.Equal("Excellent!", comment.Text);
        Assert.NotNull(comment.ModifiedOnUtc);
    }

    [Fact]
    public void UpdateText_WithSameValue_DoesNotSetModifiedOnUtc() {
        var comment = RecipeComment.Create(UserId.New(), RecipeId.New(), "Good");

        comment.UpdateText("  Good  ");

        Assert.Null(comment.ModifiedOnUtc);
    }

    [Fact]
    public void UpdateText_WithBlankText_Throws() {
        var comment = RecipeComment.Create(UserId.New(), RecipeId.New(), "Good");

        Assert.Throws<ArgumentException>(() => comment.UpdateText("   "));
    }

    [Fact]
    public void UpdateText_WithTooLongText_Throws() {
        var comment = RecipeComment.Create(UserId.New(), RecipeId.New(), "Good");

        Assert.Throws<ArgumentOutOfRangeException>(() => comment.UpdateText(new string('a', 2001)));
    }
}
