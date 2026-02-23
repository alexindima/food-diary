using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Events;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using System.Reflection;

namespace FoodDiary.Application.Tests.Domain;

public class MealInvariantTests {
    [Fact]
    public void Create_WithEmptyUserId_Throws() {
        Assert.Throws<ArgumentException>(() =>
            Meal.Create(UserId.Empty, DateTime.UtcNow, MealType.Breakfast));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10)]
    public void Create_WithOutOfRangeSatiety_Throws(int satietyLevel) {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Meal.Create(
                UserId.New(),
                DateTime.UtcNow,
                MealType.Breakfast,
                preMealSatietyLevel: satietyLevel,
                postMealSatietyLevel: 5));
    }

    [Fact]
    public void UpdateComment_WithTrimmedSameValue_DoesNotSetModifiedOnUtc() {
        var meal = Meal.Create(UserId.New(), DateTime.UtcNow, comment: "Comment");

        meal.UpdateComment("  Comment  ");

        Assert.Null(meal.ModifiedOnUtc);
    }

    [Fact]
    public void UpdateComment_WithWhitespace_ClearsValue() {
        var meal = Meal.Create(UserId.New(), DateTime.UtcNow, comment: "Comment");

        meal.UpdateComment("   ");

        Assert.Null(meal.Comment);
        Assert.NotNull(meal.ModifiedOnUtc);
    }

    [Fact]
    public void UpdateImage_WithTrimmedSameImageAndNoAssetChange_DoesNotSetModifiedOnUtc() {
        var meal = Meal.Create(UserId.New(), DateTime.UtcNow, imageUrl: "https://img", imageAssetId: ImageAssetId.New());

        meal.UpdateImage("  https://img  ", null);

        Assert.Null(meal.ModifiedOnUtc);
    }

    [Fact]
    public void UpdateImage_WithAssetOnly_UpdatesImageAssetId() {
        var initialAsset = ImageAssetId.New();
        var nextAsset = ImageAssetId.New();
        var meal = Meal.Create(UserId.New(), DateTime.UtcNow, imageUrl: "https://img", imageAssetId: initialAsset);

        meal.UpdateImage("https://img", nextAsset);

        Assert.Equal(nextAsset, meal.ImageAssetId);
        Assert.NotNull(meal.ModifiedOnUtc);
    }

    [Fact]
    public void AddProduct_WithEmptyProductId_Throws() {
        var meal = Meal.Create(UserId.New(), DateTime.UtcNow);

        Assert.Throws<ArgumentException>(() => meal.AddProduct(ProductId.Empty, 100));
    }

    [Fact]
    public void AddRecipe_WithEmptyRecipeId_Throws() {
        var meal = Meal.Create(UserId.New(), DateTime.UtcNow);

        Assert.Throws<ArgumentException>(() => meal.AddRecipe(RecipeId.Empty, 1));
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    [InlineData(1000000.0001d)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NaN)]
    public void AddProduct_WithInvalidAmount_Throws(double amount) {
        var meal = Meal.Create(UserId.New(), DateTime.UtcNow);

        Assert.Throws<ArgumentOutOfRangeException>(() => meal.AddProduct(ProductId.New(), amount));
    }

    [Fact]
    public void RemoveItem_WithNull_Throws() {
        var meal = Meal.Create(UserId.New(), DateTime.UtcNow);

        Assert.Throws<ArgumentNullException>(() => meal.RemoveItem(null!));
    }

    [Fact]
    public void RemoveItem_WhenItemNotInMeal_DoesNotSetModifiedOnUtc() {
        var meal = Meal.Create(UserId.New(), DateTime.UtcNow);
        var anotherMeal = Meal.Create(UserId.New(), DateTime.UtcNow);
        var foreignItem = anotherMeal.AddProduct(ProductId.New(), 100);

        meal.RemoveItem(foreignItem);

        Assert.Null(meal.ModifiedOnUtc);
    }

    [Fact]
    public void MealItem_UpdateAmount_WithSameValue_DoesNotSetModifiedOnUtc() {
        var meal = Meal.Create(UserId.New(), DateTime.UtcNow);
        var item = meal.AddProduct(ProductId.New(), 100);

        item.UpdateAmount(100);

        Assert.Null(item.ModifiedOnUtc);
    }

    [Fact]
    public void MealItem_UpdateAmount_WithBoundaryValue_UpdatesAmount() {
        var meal = Meal.Create(UserId.New(), DateTime.UtcNow);
        var item = meal.AddProduct(ProductId.New(), 100);

        item.UpdateAmount(1000000d);

        Assert.Equal(1000000d, item.Amount);
        Assert.NotNull(item.ModifiedOnUtc);
    }

    [Fact]
    public void ClearItems_WhenAlreadyEmpty_DoesNotSetModifiedOnUtc() {
        var meal = Meal.Create(UserId.New(), DateTime.UtcNow);

        meal.ClearItems();

        Assert.Null(meal.ModifiedOnUtc);
    }

    [Fact]
    public void ClearAiSessions_WhenAlreadyEmpty_DoesNotSetModifiedOnUtc() {
        var meal = Meal.Create(UserId.New(), DateTime.UtcNow);

        meal.ClearAiSessions();

        Assert.Null(meal.ModifiedOnUtc);
    }

    [Fact]
    public void UpdateSatietyLevels_WithOutOfRangeValue_Throws() {
        var meal = Meal.Create(UserId.New(), DateTime.UtcNow);

        Assert.Throws<ArgumentOutOfRangeException>(() => meal.UpdateSatietyLevels(10, 5));
    }

    [Fact]
    public void ApplyNutrition_WithNegativeTotal_Throws() {
        var meal = Meal.Create(UserId.New(), DateTime.UtcNow, MealType.Breakfast);

        Assert.Throws<ArgumentOutOfRangeException>(() => meal.ApplyNutrition(
            totalCalories: -1,
            totalProteins: 10,
            totalFats: 10,
            totalCarbs: 10,
            totalFiber: 1,
            totalAlcohol: 0,
            isAutoCalculated: true));
    }

    [Fact]
    public void ApplyNutrition_WithDefaultAutoValues_DoesNotSetModifiedOrEvent() {
        var meal = Meal.Create(UserId.New(), DateTime.UtcNow);

        meal.ApplyNutrition(0, 0, 0, 0, 0, 0, isAutoCalculated: true);

        Assert.Null(meal.ModifiedOnUtc);
        Assert.Empty(meal.DomainEvents);
    }

    [Fact]
    public void ApplyNutrition_WithManualMode_SetsManualValuesAndRaisesEvent() {
        var meal = Meal.Create(UserId.New(), DateTime.UtcNow);

        meal.ApplyNutrition(
            totalCalories: 500.123,
            totalProteins: 30.567,
            totalFats: 20.333,
            totalCarbs: 40.999,
            totalFiber: 8.111,
            totalAlcohol: 0,
            isAutoCalculated: false,
            manualCalories: 510,
            manualProteins: 31,
            manualFats: 21,
            manualCarbs: 41,
            manualFiber: 9,
            manualAlcohol: 0);

        Assert.False(meal.IsNutritionAutoCalculated);
        Assert.Equal(500.12, meal.TotalCalories);
        Assert.Equal(30.57, meal.TotalProteins);
        Assert.Equal(21, meal.ManualFats);
        Assert.Single(meal.DomainEvents);
        Assert.IsType<MealNutritionAppliedDomainEvent>(meal.DomainEvents[0]);
    }

    [Fact]
    public void ApplyNutrition_WithSameValues_DoesNotRaiseDuplicateEvent() {
        var meal = Meal.Create(UserId.New(), DateTime.UtcNow);
        meal.ApplyNutrition(500, 30, 20, 40, 8, 0, isAutoCalculated: true);
        var eventCount = meal.DomainEvents.Count;
        var modified = meal.ModifiedOnUtc;

        meal.ApplyNutrition(500, 30, 20, 40, 8, 0, isAutoCalculated: true);

        Assert.Equal(eventCount, meal.DomainEvents.Count);
        Assert.Equal(modified, meal.ModifiedOnUtc);
    }

    [Fact]
    public void ApplyNutrition_ManualThenAuto_ClearsManualValuesAndRaisesEvent() {
        var meal = Meal.Create(UserId.New(), DateTime.UtcNow);
        meal.ApplyNutrition(
            totalCalories: 500,
            totalProteins: 30,
            totalFats: 20,
            totalCarbs: 40,
            totalFiber: 8,
            totalAlcohol: 0,
            isAutoCalculated: false,
            manualCalories: 510,
            manualProteins: 31,
            manualFats: 21,
            manualCarbs: 41,
            manualFiber: 9,
            manualAlcohol: 0);
        meal.ClearDomainEvents();

        meal.ApplyNutrition(
            totalCalories: 500,
            totalProteins: 30,
            totalFats: 20,
            totalCarbs: 40,
            totalFiber: 8,
            totalAlcohol: 0,
            isAutoCalculated: true);

        Assert.True(meal.IsNutritionAutoCalculated);
        Assert.Null(meal.ManualCalories);
        Assert.Null(meal.ManualProteins);
        Assert.Null(meal.ManualFats);
        Assert.Null(meal.ManualCarbs);
        Assert.Null(meal.ManualFiber);
        Assert.Null(meal.ManualAlcohol);
        Assert.Single(meal.DomainEvents);
        Assert.IsType<MealNutritionAppliedDomainEvent>(meal.DomainEvents[0]);
    }

    [Fact]
    public void ApplyNutrition_WithOnlyRoundingDifference_DoesNotRaiseDuplicateEvent() {
        var meal = Meal.Create(UserId.New(), DateTime.UtcNow);
        meal.ApplyNutrition(
            totalCalories: 100.004,
            totalProteins: 30.004,
            totalFats: 20.004,
            totalCarbs: 40.004,
            totalFiber: 8.004,
            totalAlcohol: 0.004,
            isAutoCalculated: true);
        var eventCount = meal.DomainEvents.Count;
        var modified = meal.ModifiedOnUtc;

        meal.ApplyNutrition(
            totalCalories: 100.003,
            totalProteins: 30.003,
            totalFats: 20.003,
            totalCarbs: 40.003,
            totalFiber: 8.003,
            totalAlcohol: 0.003,
            isAutoCalculated: true);

        Assert.Equal(eventCount, meal.DomainEvents.Count);
        Assert.Equal(modified, meal.ModifiedOnUtc);
    }

    [Fact]
    public void AddAiSession_WithInvalidNameEn_Throws() {
        var meal = Meal.Create(UserId.New(), DateTime.UtcNow);

        Assert.Throws<ArgumentException>(() => meal.AddAiSession(
            imageAssetId: null,
            recognizedAtUtc: DateTime.UtcNow,
            notes: null,
            items: [
                MealAiItemData.Create(
                    nameEn: "   ",
                    nameLocal: null,
                    amount: 100,
                    unit: "g",
                    calories: 100,
                    proteins: 10,
                    fats: 10,
                    carbs: 10,
                    fiber: 1,
                    alcohol: 0)
            ]));
    }

    [Fact]
    public void AddAiSession_WithInvalidAmount_Throws() {
        var meal = Meal.Create(UserId.New(), DateTime.UtcNow);

        Assert.Throws<ArgumentOutOfRangeException>(() => meal.AddAiSession(
            imageAssetId: null,
            recognizedAtUtc: DateTime.UtcNow,
            notes: null,
            items: [
                MealAiItemData.Create(
                    nameEn: "Apple",
                    nameLocal: null,
                    amount: 0,
                    unit: "g",
                    calories: 100,
                    proteins: 10,
                    fats: 10,
                    carbs: 10,
                    fiber: 1,
                    alcohol: 0)
            ]));
    }

    [Fact]
    public void AddAiSession_WithNegativeNutrition_Throws() {
        var meal = Meal.Create(UserId.New(), DateTime.UtcNow);

        Assert.Throws<ArgumentOutOfRangeException>(() => meal.AddAiSession(
            imageAssetId: null,
            recognizedAtUtc: DateTime.UtcNow,
            notes: null,
            items: [
                MealAiItemData.Create(
                    nameEn: "Apple",
                    nameLocal: null,
                    amount: 100,
                    unit: "g",
                    calories: -1,
                    proteins: 10,
                    fats: 10,
                    carbs: 10,
                    fiber: 1,
                    alcohol: 0)
            ]));
    }

    [Fact]
    public void AddAiSession_WithValidItem_NormalizesFieldsAndAttachesSession() {
        var meal = Meal.Create(UserId.New(), DateTime.UtcNow);

        var session = meal.AddAiSession(
            imageAssetId: null,
            recognizedAtUtc: DateTime.UtcNow,
            notes: null,
            items: [
                MealAiItemData.Create(
                    nameEn: "  Apple  ",
                    nameLocal: "  Яблоко  ",
                    amount: 100,
                    unit: "  g  ",
                    calories: 52,
                    proteins: 0.3,
                    fats: 0.2,
                    carbs: 14,
                    fiber: 2.4,
                    alcohol: 0)
            ]);

        var aiItem = Assert.Single(session.Items);
        Assert.Equal("Apple", aiItem.NameEn);
        Assert.Equal("Яблоко", aiItem.NameLocal);
        Assert.Equal("g", aiItem.Unit);
        Assert.Equal(session.Id, aiItem.MealAiSessionId);
    }

    [Fact]
    public void AddAiSession_WithWhitespaceNotes_NormalizesToNull() {
        var meal = Meal.Create(UserId.New(), DateTime.UtcNow);

        var session = meal.AddAiSession(
            imageAssetId: null,
            recognizedAtUtc: DateTime.UtcNow,
            notes: "   ",
            items: []);

        Assert.Null(session.Notes);
    }

    [Fact]
    public void AddAiSession_WithTooLongNotes_Throws() {
        var meal = Meal.Create(UserId.New(), DateTime.UtcNow);

        Assert.Throws<ArgumentOutOfRangeException>(() => meal.AddAiSession(
            imageAssetId: null,
            recognizedAtUtc: DateTime.UtcNow,
            notes: new string('n', 2049),
            items: []));
    }

    [Fact]
    public void AddAiSession_WithLocalRecognizedAt_NormalizesToUtc() {
        var meal = Meal.Create(UserId.New(), DateTime.UtcNow);
        var localTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);

        var session = meal.AddAiSession(
            imageAssetId: null,
            recognizedAtUtc: localTime,
            notes: null,
            items: []);

        Assert.Equal(DateTimeKind.Utc, session.RecognizedAtUtc.Kind);
    }

    [Fact]
    public void AddAiSession_WithItems_SetsSessionModifiedOnUtc() {
        var meal = Meal.Create(UserId.New(), DateTime.UtcNow);

        var session = meal.AddAiSession(
            imageAssetId: null,
            recognizedAtUtc: DateTime.UtcNow,
            notes: null,
            items: [
                MealAiItemData.Create(
                    nameEn: "Apple",
                    nameLocal: null,
                    amount: 100,
                    unit: "g",
                    calories: 52,
                    proteins: 0.3,
                    fats: 0.2,
                    carbs: 14,
                    fiber: 2.4,
                    alcohol: 0)
            ]);

        Assert.NotNull(session.ModifiedOnUtc);
    }

    [Fact]
    public void MealAiItemData_TryCreate_WithInvalidData_ReturnsFalse() {
        var success = MealAiItemData.TryCreate(
            nameEn: "  ",
            nameLocal: null,
            amount: -1,
            unit: "  ",
            calories: -1,
            proteins: 0,
            fats: 0,
            carbs: 0,
            fiber: 0,
            alcohol: 0,
            data: out var data,
            error: out var error);

        Assert.False(success);
        Assert.Null(data);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void MealAiItemData_Create_WithValidData_NormalizesValues() {
        var data = MealAiItemData.Create(
            nameEn: "  Apple  ",
            nameLocal: "  Яблоко  ",
            amount: 100,
            unit: "  g  ",
            calories: 52,
            proteins: 0.3,
            fats: 0.2,
            carbs: 14,
            fiber: 2.4,
            alcohol: 0);

        Assert.Equal("Apple", data.NameEn);
        Assert.Equal("Яблоко", data.NameLocal);
        Assert.Equal("g", data.Unit);
        Assert.Equal(100, data.Amount);
    }

    [Fact]
    public void MealAiItemData_TryCreate_WithValidData_ReturnsTrue() {
        var success = MealAiItemData.TryCreate(
            nameEn: "  Apple  ",
            nameLocal: "  Яблоко  ",
            amount: 100,
            unit: "  g  ",
            calories: 52,
            proteins: 0.3,
            fats: 0.2,
            carbs: 14,
            fiber: 2.4,
            alcohol: 0,
            data: out var data,
            error: out var error);

        Assert.True(success);
        Assert.NotNull(data);
        Assert.Null(error);
        Assert.Equal("Apple", data.NameEn);
    }

    [Fact]
    public void MealAiSession_Create_WithEmptyMealId_Throws() {
        var createMethod = typeof(MealAiSession).GetMethod(
            "Create",
            BindingFlags.Static | BindingFlags.NonPublic)!;

        var ex = Assert.Throws<TargetInvocationException>(() =>
            createMethod.Invoke(null, [MealId.Empty, null, DateTime.UtcNow, null]));

        Assert.IsType<ArgumentException>(ex.InnerException);
    }

    [Fact]
    public void MealAiSession_AddItems_WithNullItem_Throws() {
        var createMethod = typeof(MealAiSession).GetMethod(
            "Create",
            BindingFlags.Static | BindingFlags.NonPublic)!;
        var addItemsMethod = typeof(MealAiSession).GetMethod(
            "AddItems",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        var itemCreateMethod = typeof(MealAiItem).GetMethod(
            "Create",
            BindingFlags.Static | BindingFlags.NonPublic)!;
        var attachMethod = typeof(MealAiItem).GetMethod(
            "AttachToSession",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        var session = (MealAiSession)createMethod.Invoke(null, [MealId.New(), null, DateTime.UtcNow, null])!;
        var item = (MealAiItem)itemCreateMethod.Invoke(
            null,
            ["Apple", null, 100d, "g", 52d, 0.3d, 0.2d, 14d, 2.4d, 0d])!;
        attachMethod.Invoke(item, [session.Id]);

        var listWithNull = new List<MealAiItem> { item, null! };

        var ex = Assert.Throws<TargetInvocationException>(() => addItemsMethod.Invoke(session, [listWithNull]));

        Assert.IsType<ArgumentException>(ex.InnerException);
    }
}



