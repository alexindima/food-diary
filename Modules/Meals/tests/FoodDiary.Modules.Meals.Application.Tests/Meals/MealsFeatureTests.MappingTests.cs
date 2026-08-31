using FoodDiary.Application.Abstractions.Meals.Models;
using FoodDiary.Application.Meals.Mappings;
using FoodDiary.Domain.Enums;
using FoodDiary.Application.Meals.Models;

namespace FoodDiary.Application.Tests.Meals;

public partial class MealsFeatureTests {

    [Fact]
    public void MealMappings_ToModel_MapsReadModelAndFavoriteState() {
        var mealId = Guid.NewGuid();
        var favoriteMealId = Guid.NewGuid();
        var meal = new MealProjectionReadModel(
            mealId,
            new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc),
            MealType.Lunch,
            "Read model meal",
            ImageUrl: null,
            ImageAssetId: null,
            TotalCalories: 500,
            TotalProteins: 30,
            TotalFats: 20,
            TotalCarbs: 45,
            TotalFiber: 8,
            TotalAlcohol: 0,
            IsNutritionAutoCalculated: true,
            ManualCalories: null,
            ManualProteins: null,
            ManualFats: null,
            ManualCarbs: null,
            ManualFiber: null,
            ManualAlcohol: null,
            PreMealSatietyLevel: 4,
            PostMealSatietyLevel: 7,
            Items: [],
            AiSessions: []);

        MealModel model = meal.ToModel(isFavorite: true, favoriteMealId: favoriteMealId);

        Assert.Multiple(
            () => Assert.Equal(mealId, model.Id),
            () => Assert.Equal("Read model meal", model.Comment),
            () => Assert.True(model.IsFavorite),
            () => Assert.Equal(favoriteMealId, model.FavoriteMealId),
            () => Assert.Equal(500, model.TotalCalories),
            () => Assert.Empty(model.Items),
            () => Assert.Empty(model.AiSessions));
    }

    [Fact]
    public void MealMappings_ToModel_MapsReadModelAiSessionsAndItems() {
        var mealId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var aiItemId = Guid.NewGuid();
        MealProjectionReadModel meal = CreateReadModelWithAiItem(mealId, sessionId, aiItemId);

        MealModel model = meal.ToModel();

        MealAiSessionModel session = Assert.Single(model.AiSessions);
        MealAiItemModel item = Assert.Single(session.Items);
        Assert.Equal(sessionId, session.Id);
        Assert.Equal("Text", session.Source);
        Assert.Equal("Completed", session.Status);
        Assert.Equal("recognized", session.Notes);
        Assert.Equal(aiItemId, item.Id);
        Assert.Equal("Soup", item.NameEn);
        Assert.Equal("Soup local", item.NameLocal);
        Assert.Equal(0.91, item.Confidence);
        Assert.Equal("Candidate", item.Resolution);
    }

}
