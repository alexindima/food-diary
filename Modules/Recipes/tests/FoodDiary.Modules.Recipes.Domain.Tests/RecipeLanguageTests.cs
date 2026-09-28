using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Recipes.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class RecipeLanguageTests {
    [Fact]
    public void ChangingLanguage_ResetsConfirmation_WhileKeepingSameLanguagePreservesIt() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 1, language: "en");
        recipe.SetLanguageConfirmation(confirmed: true);
        recipe.ChangeLanguage("en");
        Assert.True(recipe.LanguageConfirmed);

        recipe.ChangeLanguage("ru");
        Assert.Equal("ru", recipe.Language);
        Assert.False(recipe.LanguageConfirmed);

        recipe.SetLanguageConfirmation(confirmed: true);
        recipe.SetLanguageConfirmation(confirmed: false);
        recipe.SetLanguageConfirmation(confirmed: false);
        Assert.False(recipe.LanguageConfirmed);
    }

    [Theory]
    [InlineData("de")]
    [InlineData("")]
    public void InvalidLanguage_PreservesLanguageAndConfirmation(string language) {
        var recipe = Recipe.Create(UserId.New(), "Soup", 1, language: "en");
        recipe.SetLanguageConfirmation(confirmed: true);

        Assert.Throws<ArgumentOutOfRangeException>(() => recipe.ChangeLanguage(language));
        Assert.Equal("en", recipe.Language);
        Assert.True(recipe.LanguageConfirmed);
    }
}
