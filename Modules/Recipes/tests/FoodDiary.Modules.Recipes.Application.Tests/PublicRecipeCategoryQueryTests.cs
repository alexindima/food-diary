using FoodDiary.Modules.Recipes.Application.Queries.GetPublicRecipeCategories;
using ResultAssert = FoodDiary.Testing.Assertions.ResultAssert;

namespace FoodDiary.Modules.Recipes.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class PublicRecipeCategoryQueryTests {
    [Theory]
    [InlineData(null, true)]
    [InlineData("ru", true)]
    [InlineData("en", true)]
    [InlineData("de", false)]
    public void Categories_ValidateLanguage(string? language, bool valid) {
        var validator = new GetPublicRecipeCategoriesQueryValidator();
        Assert.Equal(valid, validator.Validate(new GetPublicRecipeCategoriesQuery(Language: language)).IsValid);
        Assert.False(validator.Validate(new GetPublicRecipeCategoriesQuery(new string('a', 65))).IsValid);
    }

    [Fact]
    public async Task Categories_ReturnStableCodesIndependentOfRecipeLanguageAsync() {
        var handler = new GetPublicRecipeCategoriesQueryHandler();
        IReadOnlyList<string> english = ResultAssert.Success(await handler.Handle(new GetPublicRecipeCategoriesQuery(Language: "en"), CancellationToken.None));
        IReadOnlyList<string> russian = ResultAssert.Success(await handler.Handle(new GetPublicRecipeCategoriesQuery(Language: "ru"), CancellationToken.None));
        Assert.Equal(15, english.Count);
        Assert.Equal(english, russian, StringComparer.Ordinal);
        Assert.Contains("other", english, StringComparer.Ordinal);
        Assert.Equal("soups", Assert.Single(ResultAssert.Success(await handler.Handle(new GetPublicRecipeCategoriesQuery("SOUP"), CancellationToken.None))));
    }
}
