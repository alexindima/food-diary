using FluentValidation.TestHelper;
using FoodDiary.Application.Favorites.FavoriteProducts.Commands.AddFavoriteProduct;
using FoodDiary.Application.Favorites.FavoriteRecipes.Commands.AddFavoriteRecipe;

namespace FoodDiary.Application.Tests.Favorites;

[ExcludeFromCodeCoverage]
public sealed class FavoriteCommandValidatorTests {
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public async Task AddFavoriteProductCommandValidator_RejectsInvalidPortion(double portion) {
        var command = new AddFavoriteProductCommand(
            UserId: Guid.NewGuid(),
            ProductId: Guid.NewGuid(),
            Name: null,
            PreferredPortionAmount: portion);

        TestValidationResult<AddFavoriteProductCommand> result =
            await new AddFavoriteProductCommandValidator().TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(static value => value.PreferredPortionAmount);
    }

    [Fact]
    public async Task AddFavoriteProductCommandValidator_RejectsEmptyProductId() {
        var command = new AddFavoriteProductCommand(
            UserId: Guid.NewGuid(),
            ProductId: Guid.Empty,
            Name: null,
            PreferredPortionAmount: 100);

        TestValidationResult<AddFavoriteProductCommand> result =
            await new AddFavoriteProductCommandValidator().TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(static value => value.ProductId);
    }

    [Fact]
    public async Task AddFavoriteRecipeCommandValidator_RejectsEmptyRecipeId() {
        var command = new AddFavoriteRecipeCommand(
            UserId: Guid.NewGuid(),
            RecipeId: Guid.Empty,
            Name: null);

        TestValidationResult<AddFavoriteRecipeCommand> result =
            await new AddFavoriteRecipeCommandValidator().TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(static value => value.RecipeId);
    }
}
