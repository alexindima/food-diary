using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Events;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using System.Reflection;
using FoodDiary.Domain.Entities.Meals;

namespace FoodDiary.Application.Tests.Domain;

[ExcludeFromCodeCoverage]
public class RecipeInvariantAndEventsTests {
    [Fact]
    public void Create_WithInvalidName_Throws() {
        Assert.Throws<ArgumentException>(() => Recipe.Create(
            UserId.New(),
            name: "   ",
            servings: 2));
    }

    [Fact]
    public void Create_WithEmptyUserId_Throws() {
        Assert.Throws<ArgumentException>(() => Recipe.Create(
            UserId.Empty,
            name: "Soup",
            servings: 2));
    }

    [Fact]
    public void Create_WithNonPositiveServings_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Recipe.Create(
            UserId.New(),
            name: "Soup",
            servings: 0));
    }

    [Fact]
    public void Create_WithWhitespaceOptionalFields_NormalizesToNull() {
        var recipe = Recipe.Create(
            UserId.New(),
            name: "Soup",
            servings: 2,
            description: "   ",
            comment: "   ",
            category: "   ",
            imageUrl: "   ");

        Assert.Null(recipe.Description);
        Assert.Null(recipe.Comment);
        Assert.Null(recipe.Category);
        Assert.Null(recipe.ImageUrl);
    }

    [Fact]
    public void Create_WithNameLengthAtLimit_Succeeds() {
        var recipe = Recipe.Create(
            UserId.New(),
            name: new string('n', 256),
            servings: 2);

        Assert.Equal(256, recipe.Name.Length);
    }

    [Fact]
    public void Create_WithNameLengthOverLimit_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Recipe.Create(
            UserId.New(),
            name: new string('n', 257),
            servings: 2));
    }

    [Fact]
    public void Create_WithDescriptionOverLimit_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Recipe.Create(
            UserId.New(),
            name: "Soup",
            servings: 2,
            description: new string('d', 2049)));
    }

    [Fact]
    public void Create_WithCategoryOverLimit_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Recipe.Create(
            UserId.New(),
            name: "Soup",
            servings: 2,
            category: new string('c', 129)));
    }

    [Fact]
    public void Create_WithImageUrlOverLimit_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Recipe.Create(
            UserId.New(),
            name: "Soup",
            servings: 2,
            imageUrl: new string('u', 2049)));
    }

    [Fact]
    public void Update_WithSameValues_DoesNotSetModifiedOnUtc() {
        var recipe = Recipe.Create(
            UserId.New(),
            name: "Soup",
            servings: 2,
            description: "Desc",
            comment: "Comment",
            category: "Category",
            imageUrl: "https://img",
            prepTime: 10,
            cookTime: 20,
            visibility: Visibility.Public);

        recipe.Update(new RecipeUpdate(
            Name: " Soup ",
            Description: "Desc",
            Comment: "Comment",
            Category: "Category",
            ImageUrl: " https://img ",
            PrepTime: 10,
            CookTime: 20,
            Servings: 2,
            Visibility: Visibility.Public));

        Assert.Null(recipe.ModifiedOnUtc);
    }

    [Fact]
    public void Update_WithChangedVisibilityAndTiming_UpdatesState() {
        var recipe = Recipe.Create(
            UserId.New(),
            name: "Soup",
            servings: 2,
            prepTime: 10,
            cookTime: 20,
            visibility: Visibility.Public);

        recipe.Update(new RecipeUpdate(
            PrepTime: 15,
            CookTime: 25,
            Visibility: Visibility.Private));

        Assert.Equal(15, recipe.PrepTime);
        Assert.Equal(25, recipe.CookTime);
        Assert.Equal(Visibility.Private, recipe.Visibility);
        Assert.NotNull(recipe.ModifiedOnUtc);
    }

    [Fact]
    public void Create_InitializesDefaultCountersAndNutritionState() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);

        Assert.Equal(0, recipe.UsageCount);
        Assert.True(recipe.IsNutritionAutoCalculated);
        Assert.Null(recipe.TotalCalories);
        Assert.Null(recipe.ManualCalories);
    }

    [Fact]
    public void UpdateIdentity_WithTrimmedValues_UpdatesAndNormalizes() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);

        recipe.UpdateIdentity(
            name: "  New Soup  ",
            description: "  New Desc  ",
            comment: "  ",
            category: "  Main  ");

        Assert.Equal("New Soup", recipe.Name);
        Assert.Equal("New Desc", recipe.Description);
        Assert.Null(recipe.Comment);
        Assert.Equal("Main", recipe.Category);
    }

    [Fact]
    public void UpdateIdentity_WithClearFlags_ClearsOptionalFields() {
        var recipe = Recipe.Create(
            UserId.New(),
            "Soup",
            2,
            description: "Desc",
            comment: "Comment",
            category: "Main");

        recipe.UpdateIdentity(clearDescription: true, clearComment: true, clearCategory: true);

        Assert.Null(recipe.Description);
        Assert.Null(recipe.Comment);
        Assert.Null(recipe.Category);
    }

    [Fact]
    public void UpdateIdentity_WithChangedComment_UpdatesComment() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2, comment: "Old comment");

        recipe.UpdateIdentity(comment: "  New comment  ");

        Assert.Equal("New comment", recipe.Comment);
        Assert.NotNull(recipe.ModifiedOnUtc);
    }

    [Fact]
    public void UpdateIdentity_WithClearFlagAndValue_Throws() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);

        Assert.Throws<ArgumentException>(() =>
            recipe.UpdateIdentity(description: "Desc", clearDescription: true));
    }

    [Fact]
    public void UpdateMedia_WithImageUrlOnly_PreservesImageAssetId() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2, imageAssetId: ImageAssetId.New());
        ImageAssetId? initialAssetId = recipe.ImageAssetId;

        recipe.UpdateMedia(imageUrl: " https://img ");

        Assert.Equal("https://img", recipe.ImageUrl);
        Assert.Equal(initialAssetId, recipe.ImageAssetId);
    }

    [Fact]
    public void UpdateMedia_WithImageAssetIdOnly_UpdatesImageAssetId() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);
        var imageAssetId = ImageAssetId.New();

        recipe.UpdateMedia(imageAssetId: imageAssetId);

        Assert.Equal(imageAssetId, recipe.ImageAssetId);
        Assert.NotNull(recipe.ModifiedOnUtc);
    }

    [Fact]
    public void UpdateMedia_WithClearFlags_ClearsOptionalFields() {
        var recipe = Recipe.Create(
            UserId.New(),
            "Soup",
            2,
            imageUrl: "https://img",
            imageAssetId: ImageAssetId.New());

        recipe.UpdateMedia(clearImageUrl: true, clearImageAssetId: true);

        Assert.Null(recipe.ImageUrl);
        Assert.Null(recipe.ImageAssetId);
    }

    [Fact]
    public void UpdateMedia_WithClearImageAssetIdAndValue_Throws() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);

        Assert.Throws<ArgumentException>(() =>
            recipe.UpdateMedia(imageAssetId: ImageAssetId.New(), clearImageAssetId: true));
    }

    [Fact]
    public void UpdateTimingAndServings_WithServingsOnly_PreservesTimes() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2, prepTime: 10, cookTime: 20);

        recipe.UpdateTimingAndServings(servings: 4);

        Assert.Equal(4, recipe.Servings);
        Assert.Equal(10, recipe.PrepTime);
        Assert.Equal(20, recipe.CookTime);
    }

    [Fact]
    public void UpdateTimingAndServings_WithTimesOnly_UpdatesTimes() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);

        recipe.UpdateTimingAndServings(prepTime: 5, cookTime: 15);

        Assert.Equal(5, recipe.PrepTime);
        Assert.Equal(15, recipe.CookTime);
        Assert.NotNull(recipe.ModifiedOnUtc);
    }

    [Fact]
    public void UpdateTimingAndServings_WithNegativePrepTime_Throws() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);

        Assert.Throws<ArgumentOutOfRangeException>(() => recipe.UpdateTimingAndServings(prepTime: -1));
    }

    [Fact]
    public void ChangeVisibility_WithSameValue_DoesNotSetModifiedOnUtc() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2, visibility: Visibility.Public);

        recipe.ChangeVisibility(Visibility.Public);

        Assert.Null(recipe.ModifiedOnUtc);
    }

    [Fact]
    public void ChangeVisibility_WithDifferentValue_UpdatesState() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2, visibility: Visibility.Public);

        recipe.ChangeVisibility(Visibility.Private);

        Assert.Equal(Visibility.Private, recipe.Visibility);
        Assert.NotNull(recipe.ModifiedOnUtc);
    }

    [Fact]
    public void AddStep_WithInvalidStepNumber_Throws() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            recipe.AddStep(0, "Instruction"));
    }

    [Fact]
    public void AddStep_WithDuplicateStepNumber_Throws() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);
        recipe.AddStep(1, "Step 1");

        Assert.Throws<ArgumentException>(() => recipe.AddStep(1, "Step 1 duplicate"));
    }

    [Fact]
    public void ClearSteps_WithExistingSteps_RemovesStepsAndSetsModifiedOnUtc() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);
        recipe.AddStep(1, "Step 1");
        recipe.ClearDomainEvents();

        recipe.ClearSteps();

        Assert.Empty(recipe.Steps);
        Assert.NotNull(recipe.ModifiedOnUtc);
    }

    [Fact]
    public void RemoveStep_WhenStepBelongsToRecipe_RemovesStepAndSetsModifiedOnUtc() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);
        RecipeStep step = recipe.AddStep(1, "Step 1");
        recipe.ClearDomainEvents();

        recipe.RemoveStep(step);

        Assert.Empty(recipe.Steps);
        Assert.NotNull(recipe.ModifiedOnUtc);
    }

    [Fact]
    public void AddStep_WithEmptyInstruction_Throws() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);

        Assert.Throws<ArgumentException>(() => recipe.AddStep(1, "   "));
    }

    [Fact]
    public void AddStep_WithTooLongInstruction_Throws() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);

        Assert.Throws<ArgumentOutOfRangeException>(() => recipe.AddStep(1, new string('a', 4001)));
    }

    [Fact]
    public void Step_Update_WithSameValues_DoesNotSetModifiedOnUtc() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);
        RecipeStep step = recipe.AddStep(1, "Instruction", "Title", "https://img");
        recipe.ClearDomainEvents();

        step.Update("Instruction", "Title", "https://img", imageAssetId: null);

        Assert.Null(step.ModifiedOnUtc);
    }

    [Fact]
    public void Step_Update_NormalizesAndClearsOptionalFields() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);
        RecipeStep step = recipe.AddStep(1, "Instruction", "Title", "https://img");

        step.Update("  New instruction  ", "   ", "   ", imageAssetId: null);

        Assert.Equal("New instruction", step.Instruction);
        Assert.Null(step.Title);
        Assert.Null(step.ImageUrl);
    }

    [Fact]
    public void Step_AddNestedRecipeIngredient_WithValidValues_AddsIngredient() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);
        RecipeStep step = recipe.AddStep(1, "Step");
        var nestedRecipeId = RecipeId.New();

        RecipeIngredient ingredient = step.AddNestedRecipeIngredient(nestedRecipeId, 1.5);

        Assert.Equal(nestedRecipeId, ingredient.NestedRecipeId);
        Assert.Null(ingredient.ProductId);
        Assert.Equal(1.5, ingredient.Amount);
        Assert.Single(step.Ingredients);
        Assert.NotNull(step.ModifiedOnUtc);
    }

    [Fact]
    public void Step_RemoveIngredient_WhenIngredientExists_RemovesIngredientAndSetsModifiedOnUtc() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);
        RecipeStep step = recipe.AddStep(1, "Step");
        RecipeIngredient ingredient = step.AddProductIngredient(ProductId.New(), 100);

        step.RemoveIngredient(ingredient);

        Assert.Empty(step.Ingredients);
        Assert.NotNull(step.ModifiedOnUtc);
    }

    [Fact]
    public void Step_Update_WithTitleOverLimit_Throws() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);
        RecipeStep step = recipe.AddStep(1, "Instruction");

        Assert.Throws<ArgumentOutOfRangeException>(() => step.Update("Instruction", new string('t', 257)));
    }

    [Fact]
    public void Step_AddProductIngredient_WithEmptyProductId_Throws() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);
        RecipeStep step = recipe.AddStep(1, "Step");

        Assert.Throws<ArgumentException>(() => step.AddProductIngredient(ProductId.Empty, 100));
    }

    [Fact]
    public void Step_AddNestedRecipeIngredient_WithEmptyRecipeId_Throws() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);
        RecipeStep step = recipe.AddStep(1, "Step");

        Assert.Throws<ArgumentException>(() => step.AddNestedRecipeIngredient(RecipeId.Empty, 1));
    }

    [Fact]
    public void Step_Create_WithEmptyRecipeId_Throws() {
        MethodInfo? method = typeof(RecipeStep).GetMethod(
            "Create",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

        TargetInvocationException exception = Assert.Throws<System.Reflection.TargetInvocationException>(() =>
            method!.Invoke(null, [RecipeId.Empty, 1, "Step", null, null, null]));

        Assert.IsType<ArgumentException>(exception.InnerException);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    [InlineData(1000000.0001d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Step_AddProductIngredient_WithInvalidAmount_Throws(double amount) {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);
        RecipeStep step = recipe.AddStep(1, "Step");

        Assert.Throws<ArgumentOutOfRangeException>(() => step.AddProductIngredient(ProductId.New(), amount));
    }

    [Fact]
    public void Ingredient_UpdateAmount_WithSameValue_DoesNotSetModifiedOnUtc() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);
        RecipeStep step = recipe.AddStep(1, "Step");
        RecipeIngredient ingredient = step.AddProductIngredient(ProductId.New(), 100);

        ingredient.UpdateAmount(100);

        Assert.Null(ingredient.ModifiedOnUtc);
    }

    [Fact]
    public void Ingredient_UpdateAmount_WithBoundaryValue_UpdatesAmount() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);
        RecipeStep step = recipe.AddStep(1, "Step");
        RecipeIngredient ingredient = step.AddProductIngredient(ProductId.New(), 100);

        ingredient.UpdateAmount(1000000d);

        Assert.Equal(1000000d, ingredient.Amount);
        Assert.NotNull(ingredient.ModifiedOnUtc);
    }

    [Fact]
    public void Ingredient_CreateWithRecipe_WithEmptyRecipeStepId_Throws() {
        MethodInfo? method = typeof(RecipeIngredient).GetMethod(
            "CreateWithRecipe",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

        TargetInvocationException exception = Assert.Throws<System.Reflection.TargetInvocationException>(() =>
            method!.Invoke(null, [RecipeStepId.Empty, RecipeId.New(), 1d]));

        Assert.IsType<ArgumentException>(exception.InnerException);
    }

    [Fact]
    public void ClearSteps_WhenEmpty_DoesNotSetModifiedOnUtc() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);

        recipe.ClearSteps();

        Assert.Null(recipe.ModifiedOnUtc);
    }

    [Fact]
    public void RemoveStep_WithNull_Throws() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);

        Assert.Throws<ArgumentNullException>(() => recipe.RemoveStep(null!));
    }

    [Fact]
    public void RemoveStep_WhenStepDoesNotBelongToRecipe_DoesNotSetModifiedOnUtc() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);
        var anotherRecipe = Recipe.Create(UserId.New(), "Other", 2);
        RecipeStep foreignStep = anotherRecipe.AddStep(1, "Foreign");
        anotherRecipe.ClearDomainEvents();

        recipe.RemoveStep(foreignStep);

        Assert.Null(recipe.ModifiedOnUtc);
    }

    [Fact]
    public void Step_RemoveIngredient_WithNull_Throws() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);
        RecipeStep step = recipe.AddStep(1, "Step");

        Assert.Throws<ArgumentNullException>(() => step.RemoveIngredient(null!));
    }

    [Fact]
    public void Step_RemoveIngredient_WhenMissing_DoesNotSetModifiedOnUtc() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);
        RecipeStep step = recipe.AddStep(1, "Step");
        RecipeStep otherStep = recipe.AddStep(2, "Another");
        RecipeIngredient foreignIngredient = otherStep.AddProductIngredient(ProductId.New(), 100);

        step.RemoveIngredient(foreignIngredient);

        Assert.Null(step.ModifiedOnUtc);
    }

    [Fact]
    public void SetManualNutrition_WithNegativeValue_Throws() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            recipe.SetManualNutrition(-1, 1, 1, 1, 1, 0));
    }

    [Fact]
    public void EnableAutoNutrition_WhenAlreadyAutoWithoutManualNutrition_DoesNothing() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);

        recipe.EnableAutoNutrition();

        Assert.Empty(recipe.DomainEvents);
        Assert.Null(recipe.ModifiedOnUtc);
    }

    [Fact]
    public void SetManualNutrition_WithSameValues_DoesNotRaiseDuplicateEvent() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);

        recipe.SetManualNutrition(100, 10, 10, 10, 1, 0);
        int eventCountBefore = recipe.DomainEvents.Count;
        DateTime? modifiedBefore = recipe.ModifiedOnUtc;

        recipe.SetManualNutrition(100, 10, 10, 10, 1, 0);

        Assert.Equal(eventCountBefore, recipe.DomainEvents.Count);
        Assert.Equal(modifiedBefore, recipe.ModifiedOnUtc);
    }

    [Fact]
    public void ApplyComputedNutrition_WhenManualMode_DoesNotChangeTotals() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);
        recipe.SetManualNutrition(100, 10, 10, 10, 1, 0);

        recipe.ApplyComputedNutrition(999, 99, 99, 99, 9, 0);

        Assert.Equal(100, recipe.TotalCalories);
        Assert.Equal(10, recipe.TotalProteins);
    }

    [Fact]
    public void ApplyComputedNutrition_WithSameValues_DoesNotSetModifiedOnUtc() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);

        recipe.ApplyComputedNutrition(calories: null, proteins: null, fats: null, carbs: null, fiber: null, alcohol: null);

        Assert.Null(recipe.ModifiedOnUtc);
    }

    [Fact]
    public void ApplyComputedNutrition_WithDifferentValues_UpdatesTotals() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);

        recipe.ApplyComputedNutrition(100, 10, 20, 30, 4, 0);

        Assert.Equal(100, recipe.TotalCalories);
        Assert.Equal(10, recipe.TotalProteins);
        Assert.Equal(20, recipe.TotalFats);
        Assert.Equal(30, recipe.TotalCarbs);
        Assert.Equal(4, recipe.TotalFiber);
        Assert.Equal(0, recipe.TotalAlcohol);
        Assert.NotNull(recipe.ModifiedOnUtc);
    }

    [Fact]
    public void Recipe_ManualAndAutoMode_RaiseEvents() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);

        recipe.SetManualNutrition(100, 10, 10, 10, 1, 0);
        recipe.EnableAutoNutrition();

        Assert.Contains(recipe.DomainEvents, e => e is RecipeManualNutritionSetDomainEvent);
        Assert.Contains(recipe.DomainEvents, e => e is RecipeAutoNutritionEnabledDomainEvent);
    }

    [Fact]
    public void NavigationCollections_AreExposedAsReadOnly() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);

        ICollection<MealItem> mealItems = Assert.IsAssignableFrom<ICollection<FoodDiary.Domain.Entities.Meals.MealItem>>(recipe.MealItems);
        ICollection<RecipeIngredient> nestedRecipeUsages = Assert.IsAssignableFrom<ICollection<RecipeIngredient>>(recipe.NestedRecipeUsages);
        ICollection<RecipeStep> steps = Assert.IsAssignableFrom<ICollection<RecipeStep>>(recipe.Steps);

        Assert.True(mealItems.IsReadOnly);
        Assert.True(nestedRecipeUsages.IsReadOnly);
        Assert.True(steps.IsReadOnly);
    }
}
