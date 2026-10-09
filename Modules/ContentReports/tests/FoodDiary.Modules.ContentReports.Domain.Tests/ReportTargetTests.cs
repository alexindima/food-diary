using FoodDiary.Modules.ContentReports.Domain.Contracts.Enums;
using FoodDiary.Modules.ContentReports.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.ContentReports.Domain.Entities;
using FoodDiary.Modules.RecipeCommunity.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.ContentReports.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class ReportTargetTests {
    [Fact]
    public void TargetFactories_PairKindWithOwningIdentity() {
        var recipeId = RecipeId.New();
        var commentId = RecipeCommentId.New();
        var recipe = ReportTarget.ForRecipe(recipeId);
        var comment = ReportTarget.ForComment(commentId);
        Assert.Multiple(
            () => Assert.Equal(ReportTargetType.Recipe, recipe.Kind),
            () => Assert.Equal(recipeId.Value, recipe.Id),
            () => Assert.Equal(ReportTargetType.Comment, comment.Kind),
            () => Assert.Equal(commentId.Value, comment.Id));
        var report = ContentReport.CreateWithTarget(UserId.New(), comment, "Spam");
        Assert.Equal(commentId.Value, report.TargetId);
    }

    [Fact]
    public void EmptyReadSentinel_IsRepresentableButNotMutable() {
        var sentinel = ReportTarget.FromFields(ReportTargetType.Recipe, Guid.Empty);
        Assert.Throws<ArgumentException>(() => ContentReport.CreateWithTarget(UserId.New(), sentinel, "Spam"));
        var unsupported = ReportTarget.FromFields((ReportTargetType)987, Guid.NewGuid());
        Assert.Throws<ArgumentOutOfRangeException>(() => ContentReport.CreateWithTarget(UserId.New(), unsupported, "Spam"));
    }

}
