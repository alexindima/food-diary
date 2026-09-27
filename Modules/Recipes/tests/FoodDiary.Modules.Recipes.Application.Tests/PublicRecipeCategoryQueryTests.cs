using FoodDiary.Modules.Recipes.Application.Queries.GetPublicRecipeCategories;
using FoodDiary.Modules.Recipes.Contracts.Common;
using FoodDiary.Results;
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
    public async Task Categories_ForwardSearchLanguageAndCancellationAsync() {
        IRecipeOverviewReadService read = Substitute.For<IRecipeOverviewReadService>();
        using var cancellation = new CancellationTokenSource();
        read.GetPublicCategoriesAsync("soup", "en", cancellation.Token).Returns((IReadOnlyList<string>)["Soups"]);
        Result<IReadOnlyList<string>> result = await new GetPublicRecipeCategoriesQueryHandler(read).Handle(new GetPublicRecipeCategoriesQuery("soup", "en"), cancellation.Token);
        Assert.Equal("Soups", Assert.Single(ResultAssert.Success(result)));
        await read.Received(1).GetPublicCategoriesAsync("soup", "en", cancellation.Token);
    }
}
