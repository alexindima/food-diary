using FoodDiary.Application.Abstractions.RecipeComments.Common;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class FeatureErrorContractTests {
    [Fact]
    public void RecipeCommentErrors_HasOwnerAssemblyAndNamespace() {
        Assert.Multiple(
            () => Assert.Equal("FoodDiary.Modules.RecipeCommunity.Application.Abstractions", typeof(RecipeCommentErrors).Assembly.GetName().Name),
            () => Assert.Equal("FoodDiary.Application.Abstractions.RecipeComments.Common", typeof(RecipeCommentErrors).Namespace));
    }

    [Fact]
    public void RecipeCommentErrors_PreservesEveryPublicErrorContract() {
        AssertError(RecipeCommentErrors.NotFound(Guid.Parse("12345678-1234-1234-1234-123456789abc")), "RecipeComment.NotFound", "Comment with ID 12345678-1234-1234-1234-123456789abc was not found.", ErrorKind.NotFound);
        AssertError(RecipeCommentErrors.NotAuthor, "RecipeComment.NotAuthor", "You are not the author of this comment.", ErrorKind.Forbidden);
    }

    private static void AssertError(Error error, string code, string message, ErrorKind kind) {
        Assert.Multiple(
            () => Assert.Equal(code, error.Code),
            () => Assert.Equal(message, error.Message),
            () => Assert.Equal(kind, error.Kind),
            () => Assert.Null(error.Details));
    }
}
