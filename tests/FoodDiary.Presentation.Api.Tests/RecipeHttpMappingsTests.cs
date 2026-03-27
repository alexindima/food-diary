using FoodDiary.Presentation.Api.Features.Recipes.Mappings;
using FoodDiary.Presentation.Api.Features.Recipes.Requests;

namespace FoodDiary.Presentation.Api.Tests;

public sealed class RecipeHttpMappingsTests {
    [Fact]
    public void CreateRecipeRequest_ToCommand_MapsAllFieldsAndSteps() {
        var userId = Guid.NewGuid();
        var imageAssetId = Guid.NewGuid();
        var stepAssetId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var nestedRecipeId = Guid.NewGuid();
        var request = new CreateRecipeHttpRequest(
            Name: "Tomato Soup",
            Description: "Creamy soup",
            Comment: "Serve hot",
            Category: "Lunch",
            ImageUrl: "https://cdn.example/soup.png",
            ImageAssetId: imageAssetId,
            PrepTime: 15,
            CookTime: 35,
            Servings: 4,
            Visibility: "Private",
            CalculateNutritionAutomatically: true,
            ManualCalories: 220,
            ManualProteins: 8,
            ManualFats: 6,
            ManualCarbs: 30,
            ManualFiber: 4,
            ManualAlcohol: 0,
            Steps: [
                new RecipeStepHttpRequest(
                    Title: "Prep",
                    Description: "Chop vegetables",
                    Ingredients: [
                        new RecipeIngredientHttpRequest(productId, null, 200),
                        new RecipeIngredientHttpRequest(null, nestedRecipeId, 50)
                    ],
                    ImageUrl: "https://cdn.example/step-1.png",
                    ImageAssetId: stepAssetId)
            ]);

        var command = request.ToCommand(userId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(request.Name, command.Name);
        Assert.Equal(request.Description, command.Description);
        Assert.Equal(request.Comment, command.Comment);
        Assert.Equal(request.Category, command.Category);
        Assert.Equal(request.ImageUrl, command.ImageUrl);
        Assert.Equal(request.ImageAssetId, command.ImageAssetId);
        Assert.Equal(request.PrepTime, command.PrepTime);
        Assert.Equal(request.CookTime, command.CookTime);
        Assert.Equal(request.Servings, command.Servings);
        Assert.Equal(request.Visibility, command.Visibility);
        Assert.Equal(request.CalculateNutritionAutomatically, command.CalculateNutritionAutomatically);
        Assert.Equal(request.ManualCalories, command.ManualCalories);
        Assert.Equal(request.ManualProteins, command.ManualProteins);
        Assert.Equal(request.ManualFats, command.ManualFats);
        Assert.Equal(request.ManualCarbs, command.ManualCarbs);
        Assert.Equal(request.ManualFiber, command.ManualFiber);
        Assert.Equal(request.ManualAlcohol, command.ManualAlcohol);

        var step = Assert.Single(command.Steps);
        Assert.Equal(1, step.Order);
        Assert.Equal("Prep", step.Title);
        Assert.Equal("Chop vegetables", step.Description);
        Assert.Equal("https://cdn.example/step-1.png", step.ImageUrl);
        Assert.Equal(stepAssetId, step.ImageAssetId);

        var firstIngredient = step.Ingredients[0];
        Assert.Equal(productId, firstIngredient.ProductId);
        Assert.Null(firstIngredient.NestedRecipeId);
        Assert.Equal(200, firstIngredient.Amount);

        var secondIngredient = step.Ingredients[1];
        Assert.Null(secondIngredient.ProductId);
        Assert.Equal(nestedRecipeId, secondIngredient.NestedRecipeId);
        Assert.Equal(50, secondIngredient.Amount);
    }

    [Fact]
    public void UpdateRecipeRequest_ToCommand_MapsNullableSteps() {
        var userId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();
        var imageAssetId = Guid.NewGuid();
        var request = new UpdateRecipeHttpRequest(
            Name: "Updated Soup",
            Description: "Updated description",
            ClearDescription: true,
            Comment: "Updated comment",
            ClearComment: false,
            Category: "Dinner",
            ClearCategory: true,
            ImageUrl: "https://cdn.example/updated-soup.png",
            ClearImageUrl: false,
            ImageAssetId: imageAssetId,
            ClearImageAssetId: true,
            PrepTime: 10,
            CookTime: 25,
            Servings: 2,
            Visibility: "Public",
            CalculateNutritionAutomatically: false,
            ManualCalories: 180,
            ManualProteins: 5,
            ManualFats: 7,
            ManualCarbs: 22,
            ManualFiber: 3,
            ManualAlcohol: 0,
            Steps: null);

        var command = request.ToCommand(userId, recipeId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(recipeId, command.RecipeId);
        Assert.Equal(request.Name, command.Name);
        Assert.Equal(request.Description, command.Description);
        Assert.Equal(request.ClearDescription, command.ClearDescription);
        Assert.Equal(request.Comment, command.Comment);
        Assert.Equal(request.ClearComment, command.ClearComment);
        Assert.Equal(request.Category, command.Category);
        Assert.Equal(request.ClearCategory, command.ClearCategory);
        Assert.Equal(request.ImageUrl, command.ImageUrl);
        Assert.Equal(request.ClearImageUrl, command.ClearImageUrl);
        Assert.Equal(request.ImageAssetId, command.ImageAssetId);
        Assert.Equal(request.ClearImageAssetId, command.ClearImageAssetId);
        Assert.Equal(request.PrepTime, command.PrepTime);
        Assert.Equal(request.CookTime, command.CookTime);
        Assert.Equal(request.Servings, command.Servings);
        Assert.Equal(request.Visibility, command.Visibility);
        Assert.Equal(request.CalculateNutritionAutomatically, command.CalculateNutritionAutomatically);
        Assert.Equal(request.ManualCalories, command.ManualCalories);
        Assert.Equal(request.ManualProteins, command.ManualProteins);
        Assert.Equal(request.ManualFats, command.ManualFats);
        Assert.Equal(request.ManualCarbs, command.ManualCarbs);
        Assert.Equal(request.ManualFiber, command.ManualFiber);
        Assert.Equal(request.ManualAlcohol, command.ManualAlcohol);
        Assert.Null(command.Steps);
    }
}
