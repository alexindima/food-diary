using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Events;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Tests.Domain;

public class RecipeInvariantAndEventsTests
{
    [Fact]
    public void Create_WithInvalidName_Throws()
    {
        Assert.Throws<ArgumentException>(() => Recipe.Create(
            UserId.New(),
            name: "   ",
            servings: 2));
    }

    [Fact]
    public void Create_WithNonPositiveServings_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Recipe.Create(
            UserId.New(),
            name: "Soup",
            servings: 0));
    }

    [Fact]
    public void SetManualNutrition_WithNegativeValue_Throws()
    {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            recipe.SetManualNutrition(-1, 1, 1, 1, 1, 0));
    }

    [Fact]
    public void Recipe_ManualAndAutoMode_RaiseEvents()
    {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);

        recipe.SetManualNutrition(100, 10, 10, 10, 1, 0);
        recipe.EnableAutoNutrition();

        Assert.Contains(recipe.DomainEvents, e => e is RecipeManualNutritionSetDomainEvent);
        Assert.Contains(recipe.DomainEvents, e => e is RecipeAutoNutritionEnabledDomainEvent);
    }

    [Fact]
    public void AddStep_WithInvalidStepNumber_Throws()
    {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            recipe.AddStep(0, "Instruction"));
    }
}

