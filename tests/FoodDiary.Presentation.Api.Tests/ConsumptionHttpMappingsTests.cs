using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Consumptions.Models;
using FoodDiary.Application.FavoriteMeals.Models;
using FoodDiary.Presentation.Api.Features.Consumptions.Mappings;
using FoodDiary.Presentation.Api.Features.Consumptions.Requests;

namespace FoodDiary.Presentation.Api.Tests;

public sealed class ConsumptionHttpMappingsTests {
    // ── Command mappings ──

    [Fact]
    public void ToDeleteCommand_MapsIds() {
        var userId = Guid.NewGuid();
        var consumptionId = Guid.NewGuid();

        var command = consumptionId.ToDeleteCommand(userId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(consumptionId, command.ConsumptionId);
    }

    [Fact]
    public void CreateRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var date = DateTime.UtcNow;
        var productId = Guid.NewGuid();
        var request = new CreateConsumptionHttpRequest(
            date, "Breakfast", "Tasty", null, null,
            new List<ConsumptionItemHttpRequest> {
                new(productId, null, 150),
            },
            PreMealSatietyLevel: 3,
            PostMealSatietyLevel: 7);

        var command = request.ToCommand(userId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(date, command.Date);
        Assert.Equal("Breakfast", command.MealType);
        Assert.Equal("Tasty", command.Comment);
        Assert.Single(command.Items);
        Assert.Equal(productId, command.Items[0].ProductId);
        Assert.Equal(150, command.Items[0].Amount);
        Assert.Equal(3, command.PreMealSatietyLevel);
        Assert.Equal(7, command.PostMealSatietyLevel);
    }

    [Fact]
    public void CreateRequest_ToCommand_WithAiSessions_MapsNestedItems() {
        var userId = Guid.NewGuid();
        var assetId = Guid.NewGuid();
        var recognizedAt = DateTime.UtcNow;
        var request = new CreateConsumptionHttpRequest(
            DateTime.UtcNow, null, null, null, null,
            [],
            new List<ConsumptionAiSessionHttpRequest> {
                new(assetId, "Photo", recognizedAt, "AI notes", new List<ConsumptionAiItemHttpRequest> {
                    new("Chicken breast", "Куриная грудка", 200, "g", 330, 62, 7.2, 0, 0, 0),
                }),
            });

        var command = request.ToCommand(userId);

        Assert.Single(command.AiSessions);
        Assert.Equal(assetId, command.AiSessions[0].ImageAssetId);
        Assert.Equal(recognizedAt, command.AiSessions[0].RecognizedAtUtc);
        Assert.Equal("AI notes", command.AiSessions[0].Notes);
        Assert.Single(command.AiSessions[0].Items);
        Assert.Equal("Chicken breast", command.AiSessions[0].Items[0].NameEn);
        Assert.Equal("Куриная грудка", command.AiSessions[0].Items[0].NameLocal);
        Assert.Equal(200, command.AiSessions[0].Items[0].Amount);
        Assert.Equal(330, command.AiSessions[0].Items[0].Calories);
    }

    [Fact]
    public void CreateRequest_ToCommand_WithNullAiSessions_MapsToEmptyList() {
        var request = new CreateConsumptionHttpRequest(DateTime.UtcNow, null, null, null, null, []);

        var command = request.ToCommand(Guid.NewGuid());

        Assert.Empty(command.AiSessions);
    }

    [Fact]
    public void UpdateRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var consumptionId = Guid.NewGuid();
        var request = new UpdateConsumptionHttpRequest(
            DateTime.UtcNow, "Lunch", null, null, null,
            new List<ConsumptionItemHttpRequest> { new(null, Guid.NewGuid(), 2) },
            ManualCalories: 500, ManualProteins: 30, ManualFats: 20, ManualCarbs: 60,
            ManualFiber: 5, ManualAlcohol: 0, IsNutritionAutoCalculated: false);

        var command = request.ToCommand(userId, consumptionId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(consumptionId, command.ConsumptionId);
        Assert.False(command.IsNutritionAutoCalculated);
        Assert.Equal(500, command.ManualCalories);
    }

    [Fact]
    public void RepeatMealRequest_ToRepeatCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var mealId = Guid.NewGuid();
        var targetDate = DateTime.UtcNow.AddDays(1);
        var request = new RepeatMealHttpRequest(targetDate, "Dinner");

        var command = request.ToRepeatCommand(userId, mealId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(mealId, command.MealId);
        Assert.Equal(targetDate, command.TargetDate);
        Assert.Equal("Dinner", command.MealType);
    }

    // ── Query mappings ──

    [Fact]
    public void GetConsumptionsHttpQuery_ToQuery_MapsAllFields() {
        var userId = Guid.NewGuid();
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;
        var httpQuery = new GetConsumptionsHttpQuery(2, 20, from, to);

        var query = httpQuery.ToQuery(userId);

        Assert.Equal(userId, query.UserId);
        Assert.Equal(2, query.Page);
        Assert.Equal(20, query.Limit);
        Assert.Equal(from, query.DateFrom);
        Assert.Equal(to, query.DateTo);
    }

    [Fact]
    public void ConsumptionId_ToQuery_MapsIds() {
        var userId = Guid.NewGuid();
        var consumptionId = Guid.NewGuid();

        var query = consumptionId.ToQuery(userId);

        Assert.Equal(userId, query.UserId);
        Assert.Equal(consumptionId, query.ConsumptionId);
    }

    // ── Response mappings ──

    [Fact]
    public void ConsumptionModel_ToHttpResponse_MapsTopLevelFields() {
        var id = Guid.NewGuid();
        var date = DateTime.UtcNow;
        var model = new ConsumptionModel(
            id, date, "Breakfast", "Comment", null, null,
            500, 30, 20, 60, 5, 0, true, null, null, null, null, null, null,
            3, 7, 72, "green", false, null, [], []);

        var response = model.ToHttpResponse();

        Assert.Equal(id, response.Id);
        Assert.Equal(date, response.Date);
        Assert.Equal("Breakfast", response.MealType);
        Assert.Equal(500, response.TotalCalories);
        Assert.Equal(3, response.PreMealSatietyLevel);
        Assert.Equal(7, response.PostMealSatietyLevel);
        Assert.Equal(72, response.QualityScore);
        Assert.Equal("green", response.QualityGrade);
        Assert.True(response.IsNutritionAutoCalculated);
        Assert.Empty(response.Items);
        Assert.Empty(response.AiSessions);
    }

    [Fact]
    public void ConsumptionModel_ToHttpResponse_MapsNestedItemsAndAiSessions() {
        var itemId = Guid.NewGuid();
        var consumptionId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var aiItemId = Guid.NewGuid();

        var items = new List<ConsumptionItemModel> {
            new(itemId, consumptionId, 150, productId, "Chicken", "g", 100, 165, 31, 3.6, 0, 0, 0, null, null, null, null, null, null, null, null, null, 85, "A"),
        };
        var aiItems = new List<ConsumptionAiItemModel> {
            new(aiItemId, sessionId, "Rice", "Рис", 200, "g", 260, 5, 0.6, 56, 0.4, 0),
        };
        var sessions = new List<ConsumptionAiSessionModel> {
            new(sessionId, consumptionId, null, null, "Text", DateTime.UtcNow, "Notes", aiItems),
        };

        var model = new ConsumptionModel(
            consumptionId, DateTime.UtcNow, null, null, null, null,
            425, 36, 4.2, 56, 0.4, 0, true, null, null, null, null, null, null,
            0, 0, 61, "yellow", false, null, items, sessions);

        var response = model.ToHttpResponse();

        Assert.Single(response.Items);
        Assert.Equal(itemId, response.Items[0].Id);
        Assert.Equal(productId, response.Items[0].ProductId);
        Assert.Equal("Chicken", response.Items[0].ProductName);
        Assert.Equal(85, response.Items[0].ProductQualityScore);
        Assert.Equal("A", response.Items[0].ProductQualityGrade);

        Assert.Single(response.AiSessions);
        Assert.Equal(sessionId, response.AiSessions[0].Id);
        Assert.Single(response.AiSessions[0].Items);
        Assert.Equal("Rice", response.AiSessions[0].Items[0].NameEn);
        Assert.Equal("Рис", response.AiSessions[0].Items[0].NameLocal);
        Assert.Equal(260, response.AiSessions[0].Items[0].Calories);
    }

    [Fact]
    public void ConsumptionModel_ToHttpResponse_WithManualNutrition() {
        var model = new ConsumptionModel(
            Guid.NewGuid(), DateTime.UtcNow, null, null, null, null,
            0, 0, 0, 0, 0, 0, false, 500, 30, 20, 60, 5, 0,
            0, 0, 55, "yellow", false, null, [], []);

        var response = model.ToHttpResponse();

        Assert.False(response.IsNutritionAutoCalculated);
        Assert.Equal(500, response.ManualCalories);
        Assert.Equal(30, response.ManualProteins);
    }

    [Fact]
    public void ConsumptionModel_ToHttpResponse_MapsFavoriteFields() {
        var favoriteMealId = Guid.NewGuid();
        var model = new ConsumptionModel(
            Guid.NewGuid(),
            DateTime.UtcNow,
            "Dinner",
            "Comment",
            null,
            null,
            500,
            30,
            20,
            60,
            5,
            0,
            true,
            null,
            null,
            null,
            null,
            null,
            null,
            3,
            7,
            70,
            "green",
            true,
            favoriteMealId,
            [],
            []);

        var response = model.ToHttpResponse();

        Assert.True(response.IsFavorite);
        Assert.Equal(favoriteMealId, response.FavoriteMealId);
    }

    [Fact]
    public void ConsumptionOverviewModel_ToHttpResponse_MapsNestedCollections() {
        var consumption = new ConsumptionModel(
            Guid.NewGuid(),
            DateTime.UtcNow,
            "Breakfast",
            null,
            null,
            null,
            350,
            20,
            12,
            30,
            4,
            0,
            true,
            null,
            null,
            null,
            null,
            null,
            null,
            2,
            6,
            66,
            "yellow",
            false,
            null,
            [],
            []);
        var favorite = new FavoriteMealModel(
            Guid.NewGuid(),
            consumption.Id,
            "Morning meal",
            DateTime.UtcNow,
            consumption.Date,
            consumption.MealType,
            consumption.TotalCalories,
            consumption.TotalProteins,
            consumption.TotalFats,
            consumption.TotalCarbs,
            1);
        var overview = new ConsumptionOverviewModel(
            new PagedResponse<ConsumptionModel>([consumption], 1, 10, 1, 1),
            [favorite],
            1);

        var response = overview.ToHttpResponse();

        Assert.Single(response.AllConsumptions.Data);
        Assert.Single(response.FavoriteItems);
        Assert.Equal(1, response.FavoriteTotalCount);
        Assert.Equal(consumption.Id, response.AllConsumptions.Data[0].Id);
        Assert.Equal(favorite.Id, response.FavoriteItems[0].Id);
    }
}
