using System.Reflection;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.Events;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class MealExtractedInvariantTests {
    private static readonly DateTime Now = new(2026, 8, 19, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void MealNutritionAppliedDomainEvent_WithOverride_ExposesNutritionValues() {
        var occurredOnUtc = new DateTime(2026, 3, 27, 12, 0, 0, DateTimeKind.Utc);
        var mealId = MealId.New();

        var domainEvent = new MealNutritionAppliedDomainEvent(
            mealId,
            isAutoCalculated: true,
            totalCalories: 500,
            totalProteins: 30,
            totalFats: 20,
            totalCarbs: 50,
            totalFiber: 5,
            totalAlcohol: 0,
            occurredOnUtc);

        Assert.Multiple(
            () => Assert.Equal(mealId, domainEvent.MealId),
            () => Assert.True(domainEvent.IsAutoCalculated),
            () => Assert.Equal(500, domainEvent.TotalCalories),
            () => Assert.Equal(30, domainEvent.TotalProteins),
            () => Assert.Equal(20, domainEvent.TotalFats),
            () => Assert.Equal(50, domainEvent.TotalCarbs),
            () => Assert.Equal(5, domainEvent.TotalFiber),
            () => Assert.Equal(0, domainEvent.TotalAlcohol),
            () => Assert.Equal(occurredOnUtc, domainEvent.OccurredOnUtc));
    }

    [Fact]
    public void Meal_ApplyNutrition_RaisesDomainEvent() {
        var meal = Meal.Create(UserId.New(), DateTime.UtcNow, MealType.Dinner);

        meal.ApplyNutrition(new MealNutritionUpdate(
            TotalCalories: 400,
            TotalProteins: 20,
            TotalFats: 10,
            TotalCarbs: 40,
            TotalFiber: 5,
            TotalAlcohol: 0,
            IsAutoCalculated: true));

        MealNutritionAppliedDomainEvent evt = Assert.Single(meal.DomainEvents.OfType<MealNutritionAppliedDomainEvent>());
        Assert.Equal(meal.Id, evt.MealId);
        Assert.True(evt.IsAutoCalculated);
    }

    [Fact]
    public void MealAiData_PublicConstructorValidatesAndFailedSessionAdditionIsAtomic() {
        ConstructorInfo[] constructors = typeof(MealAiItemData).GetConstructors(BindingFlags.Instance | BindingFlags.Public);
        var meal = Meal.Create(UserId.New(), Now);

        Assert.Single(constructors);
        Assert.Throws<ArgumentOutOfRangeException>(() => new MealAiItemData(
            "Apple",
            nameLocal: null,
            amount: double.NaN,
            "g",
            calories: 52,
            proteins: 0.3,
            fats: 0.2,
            carbs: 14,
            fiber: 2.4,
            alcohol: 0));
        Assert.Throws<ArgumentException>(() => meal.AddAiSession(
            imageAssetId: null,
            AiRecognitionSource.Text,
            Now,
            notes: null,
            [null!]));
        Assert.Empty(meal.AiSessions);
    }

    [Fact]
    public void MealItemSnapshot_WhenLateValidationFails_IsAtomicAndRejectsInvalidServings() {
        var meal = Meal.Create(UserId.New(), Now);
        MealItem item = meal.AddProduct(ProductId.New(), 100);
        item.ApplyProductSnapshot(
            "Original",
            imageUrl: null,
            MeasurementUnit.G,
            baseAmount: 100,
            caloriesPerBase: 120,
            proteinsPerBase: 10,
            fatsPerBase: 5,
            carbsPerBase: 20,
            fiberPerBase: 2,
            alcoholPerBase: 0);

        Assert.Throws<ArgumentOutOfRangeException>(() => item.ApplyProductSnapshot(
            "Changed",
            "https://example.com/changed.png",
            MeasurementUnit.Pcs,
            baseAmount: 1,
            caloriesPerBase: 200,
            proteinsPerBase: 20,
            fatsPerBase: 10,
            carbsPerBase: 30,
            fiberPerBase: 3,
            alcoholPerBase: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => item.ApplyRecipeSnapshot(
            "Recipe",
            imageUrl: null,
            servings: 0,
            totalCalories: 100,
            totalProteins: 10,
            totalFats: 5,
            totalCarbs: 20,
            totalFiber: 2,
            totalAlcohol: 0));

        Assert.Multiple(
            () => Assert.Equal("Original", item.SnapshotName),
            () => Assert.Null(item.SnapshotImageUrl),
            () => Assert.Equal(MeasurementUnit.G.ToString(), item.SnapshotUnit),
            () => Assert.Equal(100, item.SnapshotBaseAmount),
            () => Assert.Equal(120, item.SnapshotCaloriesPerBase),
            () => Assert.Equal(0, item.SnapshotAlcoholPerBase));
    }

    [Fact]
    public void MealAiItemState_Create_WithBlankNameEn_Throws() {
        Assert.Throws<ArgumentException>(() =>
            MealAiItemState.Create("   ", nameLocal: null, 100, "g", 100, 10, 5, 20, 3, 0));
    }

    [Fact]
    public void MealAiItemState_Create_WithTooLongNameEn_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MealAiItemState.Create(new string('a', 257), nameLocal: null, 100, "g", 100, 10, 5, 20, 3, 0));
    }

    [Fact]
    public void MealAiItemState_Create_WithZeroAmount_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MealAiItemState.Create("Chicken", nameLocal: null, 0, "g", 100, 10, 5, 20, 3, 0));
    }

    [Fact]
    public void MealAiItemState_Create_WithNegativeAmount_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MealAiItemState.Create("Chicken", nameLocal: null, -1, "g", 100, 10, 5, 20, 3, 0));
    }

    [Fact]
    public void MealAiItemState_Create_WithNaNAmount_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MealAiItemState.Create("Chicken", nameLocal: null, double.NaN, "g", 100, 10, 5, 20, 3, 0));
    }

    [Fact]
    public void MealAiItemState_Create_WithNegativeNutrition_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MealAiItemState.Create("Chicken", nameLocal: null, 100, "g", -1, 10, 5, 20, 3, 0));
    }

    [Fact]
    public void MealAiItemState_Create_WithInfiniteNutrition_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MealAiItemState.Create("Chicken", nameLocal: null, 100, "g", double.PositiveInfinity, 10, 5, 20, 3, 0));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void MealAiItemState_Create_WithNonFiniteConfidence_Throws(double confidence) {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MealAiItemState.Create("Chicken", nameLocal: null, 100, "g", 100, 10, 5, 20, 3, 0, confidence));
    }

    [Fact]
    public void MealAiItemState_Create_TrimsAndNormalizesText() {
        var state = MealAiItemState.Create(
            "  Chicken  ", "  Курица  ", 100, "  g  ", 165, 31, 3.6, 0, 0, 0);

        Assert.Multiple(
            () => Assert.Equal("Chicken", state.NameEn),
            () => Assert.Equal("Курица", state.NameLocal),
            () => Assert.Equal("g", state.Unit));
    }

    [Fact]
    public void MealAiItemState_Create_WithWhitespaceNameLocal_SetsNull() {
        var state = MealAiItemState.Create(
            "Chicken", "   ", 100, "g", 165, 31, 3.6, 0, 0, 0);

        Assert.Null(state.NameLocal);
    }
}
