using FoodDiary.Application.Meals.Models;
using FoodDiary.Application.Products.Products.Models;
using FoodDiary.Application.Recipes.Recipes.Models;
using FoodDiary.Presentation.Api.Features.Meals;
using FoodDiary.Presentation.Api.Features.Meals.Requests;
using FoodDiary.Presentation.Api.Features.Meals.Responses;
using FoodDiary.Presentation.Api.Features.Products;
using FoodDiary.Presentation.Api.Features.Products.Requests;
using FoodDiary.Presentation.Api.Features.Products.Responses;
using FoodDiary.Presentation.Api.Features.Recipes;
using FoodDiary.Presentation.Api.Features.Recipes.Requests;
using FoodDiary.Presentation.Api.Features.Recipes.Responses;
using FoodDiary.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class CreatedControllerRouteValueTests {
    private static readonly DateTime UtcNow = new(2026, 8, 18, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ProductsCreate_ReturnsCreatedAtGetByIdWithCreatedProductId() {
        ProductModel model = CreateProductModel();
        ProductsController controller = CreateController(
            new ProductsController(SubstituteSender.Create(Result.Success(model))));
        var request = new CreateProductHttpRequest(
            Barcode: null,
            Name: "Product",
            Brand: null,
            ProductType: "Food",
            Category: null,
            Description: null,
            Comment: null,
            ImageUrl: null,
            ImageAssetId: null,
            BaseUnit: "g",
            BaseAmount: 100,
            DefaultPortionAmount: 100,
            CaloriesPerBase: 100,
            ProteinsPerBase: 10,
            FatsPerBase: 5,
            CarbsPerBase: 12,
            FiberPerBase: 2,
            AlcoholPerBase: 0,
            Visibility: "Private");

        IActionResult result = await controller.Create(Guid.NewGuid(), request);

        CreatedAtActionResult created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Multiple(
            () => Assert.Equal(nameof(ProductsController.GetById), created.ActionName),
            () => Assert.Equal(model.Id, created.RouteValues!["id"]),
            () => Assert.Equal(model.Id, Assert.IsType<ProductHttpResponse>(created.Value).Id));
    }

    [Fact]
    public async Task MealsCreateAndRepeat_ReturnCreatedAtGetByIdWithCreatedMealId() {
        MealModel model = CreateMealModel();
        MealsController controller = CreateController(
            new MealsController(SubstituteSender.Create(Result.Success(model))));
        var createRequest = new CreateMealHttpRequest(
            UtcNow,
            "Lunch",
            Comment: null,
            ImageUrl: null,
            ImageAssetId: null,
            Items: []);

        IActionResult createResult = await controller.Create(Guid.NewGuid(), createRequest);
        IActionResult repeatResult = await controller.Repeat(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new RepeatMealHttpRequest(UtcNow.AddDays(1), "Dinner"));

        AssertCreatedMeal(createResult, model.Id);
        AssertCreatedMeal(repeatResult, model.Id);
    }

    [Fact]
    public async Task RecipesCreate_ReturnsCreatedAtGetByIdWithCreatedRecipeId() {
        RecipeModel model = CreateRecipeModel();
        RecipesController controller = CreateController(
            new RecipesController(SubstituteSender.Create(Result.Success(model))));
        var request = new CreateRecipeHttpRequest(
            "Recipe",
            Description: null,
            Comment: null,
            Category: null,
            ImageUrl: null,
            ImageAssetId: null,
            PrepTime: null,
            CookTime: null,
            Servings: 1,
            Visibility: "Private",
            CalculateNutritionAutomatically: true,
            ManualCalories: null,
            ManualProteins: null,
            ManualFats: null,
            ManualCarbs: null,
            ManualFiber: null,
            ManualAlcohol: null,
            Steps: []);

        IActionResult result = await controller.Create(Guid.NewGuid(), request);

        CreatedAtActionResult created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Multiple(
            () => Assert.Equal(nameof(RecipesController.GetById), created.ActionName),
            () => Assert.Equal(model.Id, created.RouteValues!["id"]),
            () => Assert.Equal(model.Id, Assert.IsType<RecipeHttpResponse>(created.Value).Id));
    }

    private static void AssertCreatedMeal(IActionResult result, Guid expectedId) {
        CreatedAtActionResult created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Multiple(
            () => Assert.Equal(nameof(MealsController.GetById), created.ActionName),
            () => Assert.Equal(expectedId, created.RouteValues!["id"]),
            () => Assert.Equal(expectedId, Assert.IsType<MealHttpResponse>(created.Value).Id));
    }

    private static T CreateController<T>(T controller) where T : ControllerBase {
        controller.ControllerContext = new ControllerContext {
            HttpContext = new DefaultHttpContext(),
        };
        return controller;
    }

    private static ProductModel CreateProductModel() =>
        new(
            Guid.NewGuid(),
            Barcode: null,
            Name: "Product",
            Brand: null,
            ProductType: "Food",
            Category: null,
            Description: null,
            Comment: null,
            ImageUrl: null,
            ImageAssetId: null,
            BaseUnit: "g",
            BaseAmount: 100,
            DefaultPortionAmount: 100,
            CaloriesPerBase: 100,
            ProteinsPerBase: 10,
            FatsPerBase: 5,
            CarbsPerBase: 12,
            FiberPerBase: 2,
            AlcoholPerBase: 0,
            UsageCount: 1,
            Visibility: "Private",
            CreatedAt: UtcNow,
            IsOwnedByCurrentUser: true,
            QualityScore: 80,
            QualityGrade: "green",
            UsdaFdcId: null,
            IsFavorite: false,
            FavoriteProductId: null);

    private static MealModel CreateMealModel() =>
        new(
            Guid.NewGuid(),
            UtcNow,
            "Lunch",
            Comment: null,
            ImageUrl: null,
            ImageAssetId: null,
            TotalCalories: 500,
            TotalProteins: 30,
            TotalFats: 20,
            TotalCarbs: 60,
            TotalFiber: 5,
            TotalAlcohol: 0,
            IsNutritionAutoCalculated: true,
            ManualCalories: null,
            ManualProteins: null,
            ManualFats: null,
            ManualCarbs: null,
            ManualFiber: null,
            ManualAlcohol: null,
            PreMealSatietyLevel: 3,
            PostMealSatietyLevel: 7,
            QualityScore: 80,
            QualityGrade: "green",
            IsFavorite: false,
            FavoriteMealId: null,
            Items: [],
            AiSessions: []);

    private static RecipeModel CreateRecipeModel() =>
        new(
            Guid.NewGuid(),
            "Recipe",
            Description: null,
            Comment: null,
            Category: null,
            ImageUrl: null,
            ImageAssetId: null,
            PrepTime: null,
            CookTime: null,
            Servings: 1,
            TotalCalories: 500,
            TotalProteins: 30,
            TotalFats: 20,
            TotalCarbs: 60,
            TotalFiber: 5,
            TotalAlcohol: 0,
            IsNutritionAutoCalculated: true,
            ManualCalories: null,
            ManualProteins: null,
            ManualFats: null,
            ManualCarbs: null,
            ManualFiber: null,
            ManualAlcohol: null,
            Visibility: "Private",
            UsageCount: 1,
            CreatedAt: UtcNow,
            IsOwnedByCurrentUser: true,
            QualityScore: 80,
            QualityGrade: "green",
            Steps: [],
            IsFavorite: false,
            FavoriteRecipeId: null);
}
