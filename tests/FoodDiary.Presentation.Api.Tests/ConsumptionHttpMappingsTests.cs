using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Consumptions.Commands.CreateConsumption;
using FoodDiary.Application.Consumptions.Commands.DeleteConsumption;
using FoodDiary.Application.Consumptions.Commands.RepeatMeal;
using FoodDiary.Application.Consumptions.Commands.UpdateConsumption;
using FoodDiary.Application.Consumptions.Models;
using FoodDiary.Application.Consumptions.Queries.GetConsumptionById;
using FoodDiary.Application.Consumptions.Queries.GetConsumptions;
using FoodDiary.Application.Consumptions.Queries.GetConsumptionsOverview;
using FoodDiary.Application.FavoriteMeals.Models;
using FoodDiary.Presentation.Api.Features.Consumptions.Mappings;
using FoodDiary.Presentation.Api.Features.Consumptions.Requests;
using FoodDiary.Presentation.Api.Features.Consumptions.Responses;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class ConsumptionHttpMappingsTests {
    // â”€â”€ Command mappings â”€â”€

    [Fact]
    public void ToDeleteCommand_MapsIds() {
        var userId = Guid.NewGuid();
        var consumptionId = Guid.NewGuid();

        DeleteConsumptionCommand command = consumptionId.ToDeleteCommand(userId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(consumptionId, command.ConsumptionId);
    }

    [Fact]
    public void CreateRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        DateTime date = DateTime.UtcNow;
        var productId = Guid.NewGuid();
        var request = new CreateConsumptionHttpRequest(
            date, "Breakfast", "Tasty", ImageUrl: null, ImageAssetId: null,
            new List<ConsumptionItemHttpRequest> {
                new(productId, null, 150),
            },
            PreMealSatietyLevel: 3,
            PostMealSatietyLevel: 4);

        CreateConsumptionCommand command = request.ToCommand(userId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(date, command.Date);
        Assert.Equal("Breakfast", command.MealType);
        Assert.Equal("Tasty", command.Comment);
        Assert.Single(command.Items);
        Assert.Equal(productId, command.Items[0].ProductId);
        Assert.Equal(150, command.Items[0].Amount);
        Assert.Equal(3, command.PreMealSatietyLevel);
        Assert.Equal(4, command.PostMealSatietyLevel);
    }

    [Fact]
    public void CreateRequest_ToCommand_WithAiSessions_MapsNestedItems() {
        var userId = Guid.NewGuid();
        var assetId = Guid.NewGuid();
        DateTime recognizedAt = DateTime.UtcNow;
        var request = new CreateConsumptionHttpRequest(
            DateTime.UtcNow, MealType: null, Comment: null, ImageUrl: null, ImageAssetId: null,
            [],
            new List<ConsumptionAiSessionHttpRequest> {
                new(assetId, "Photo", recognizedAt, "AI notes", new List<ConsumptionAiItemHttpRequest> {
                    new("Chicken breast", "ÐšÑƒÑ€Ð¸Ð½Ð°Ñ Ð³Ñ€ÑƒÐ´ÐºÐ°", 200, "g", 330, 62, 7.2, 0, 0, 0),
                }),
            });

        CreateConsumptionCommand command = request.ToCommand(userId);

        Assert.Single(command.AiSessions);
        Assert.Equal(assetId, command.AiSessions[0].ImageAssetId);
        Assert.Equal(recognizedAt, command.AiSessions[0].RecognizedAtUtc);
        Assert.Equal("AI notes", command.AiSessions[0].Notes);
        Assert.Single(command.AiSessions[0].Items);
        Assert.Equal("Chicken breast", command.AiSessions[0].Items[0].NameEn);
        Assert.Equal("ÐšÑƒÑ€Ð¸Ð½Ð°Ñ Ð³Ñ€ÑƒÐ´ÐºÐ°", command.AiSessions[0].Items[0].NameLocal);
        Assert.Equal(200, command.AiSessions[0].Items[0].Amount);
        Assert.Equal(330, command.AiSessions[0].Items[0].Calories);
    }

    [Fact]
    public void CreateRequest_ToCommand_WithNullAiSessions_MapsToEmptyList() {
        var request = new CreateConsumptionHttpRequest(DateTime.UtcNow, MealType: null, Comment: null, ImageUrl: null, ImageAssetId: null, []);

        CreateConsumptionCommand command = request.ToCommand(Guid.NewGuid());

        Assert.Empty(command.AiSessions);
    }

    [Fact]
    public void UpdateRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var consumptionId = Guid.NewGuid();
        var request = new UpdateConsumptionHttpRequest(
            DateTime.UtcNow,
            "Lunch",
            Comment: null,
            ImageUrl: null,
            ImageAssetId: null,
            new List<ConsumptionItemHttpRequest> { new(null, Guid.NewGuid(), 2) },
            IsNutritionAutoCalculated: false,
            ManualCalories: 500,
            ManualProteins: 30,
            ManualFats: 20,
            ManualCarbs: 60,
            ManualFiber: 5,
            ManualAlcohol: 0);

        UpdateConsumptionCommand command = request.ToCommand(userId, consumptionId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(consumptionId, command.ConsumptionId);
        Assert.False(command.IsNutritionAutoCalculated);
        Assert.Equal(500, command.ManualCalories);
    }

    [Fact]
    public void RepeatMealRequest_ToRepeatCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var mealId = Guid.NewGuid();
        DateTime targetDate = DateTime.UtcNow.AddDays(1);
        var request = new RepeatMealHttpRequest(targetDate, "Dinner");

        RepeatMealCommand command = request.ToRepeatCommand(userId, mealId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(mealId, command.MealId);
        Assert.Equal(targetDate, command.TargetDate);
        Assert.Equal("Dinner", command.MealType);
    }

    // â”€â”€ Query mappings â”€â”€

    [Fact]
    public void GetConsumptionsHttpQuery_ToQuery_MapsAllFields() {
        var userId = Guid.NewGuid();
        DateTime from = DateTime.UtcNow.AddDays(-7);
        DateTime to = DateTime.UtcNow;
        var httpQuery = new GetConsumptionsHttpQuery(2, 20, from, to);

        GetConsumptionsQuery query = httpQuery.ToQuery(userId);

        Assert.Equal(userId, query.UserId);
        Assert.Equal(2, query.Page);
        Assert.Equal(20, query.Limit);
        Assert.Equal(from, query.DateFrom);
        Assert.Equal(to, query.DateTo);
    }

    [Fact]
    public void GetConsumptionsOverviewHttpQuery_ToQuery_NormalizesPagingAndFavoriteLimit() {
        var userId = Guid.NewGuid();
        DateTime from = DateTime.UtcNow.AddDays(-30);
        DateTime to = DateTime.UtcNow;
        var httpQuery = new GetConsumptionsOverviewHttpQuery(
            Page: 0,
            Limit: 500,
            DateFrom: from,
            DateTo: to,
            FavoriteLimit: 0);

        GetConsumptionsOverviewQuery query = httpQuery.ToQuery(userId);

        Assert.Equal(userId, query.UserId);
        Assert.Equal(1, query.Page);
        Assert.Equal(100, query.Limit);
        Assert.Equal(from, query.DateFrom);
        Assert.Equal(to, query.DateTo);
        Assert.Equal(1, query.FavoriteLimit);
    }

    [Fact]
    public void ConsumptionId_ToQuery_MapsIds() {
        var userId = Guid.NewGuid();
        var consumptionId = Guid.NewGuid();

        GetConsumptionByIdQuery query = consumptionId.ToQuery(userId);

        Assert.Equal(userId, query.UserId);
        Assert.Equal(consumptionId, query.ConsumptionId);
    }

    // â”€â”€ Response mappings â”€â”€

    [Fact]
    public void ConsumptionModel_ToHttpResponse_MapsTopLevelFields() {
        var id = Guid.NewGuid();
        DateTime date = DateTime.UtcNow;
        var model = new ConsumptionModel(
            id, date, "Breakfast", "Comment", ImageUrl: null, ImageAssetId: null,
            500, 30, 20, 60, 5, 0, IsNutritionAutoCalculated: true, ManualCalories: null, ManualProteins: null, ManualFats: null, ManualCarbs: null, ManualFiber: null, ManualAlcohol: null,
            3, 4, 72, "green", IsFavorite: false, FavoriteMealId: null, [], []);

        ConsumptionHttpResponse response = model.ToHttpResponse();

        Assert.Equal(id, response.Id);
        Assert.Equal(date, response.Date);
        Assert.Equal("Breakfast", response.MealType);
        Assert.Equal(500, response.TotalCalories);
        Assert.Equal(3, response.PreMealSatietyLevel);
        Assert.Equal(4, response.PostMealSatietyLevel);
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
            new(
                itemId,
                consumptionId,
                150,
                productId,
                "Chicken",
                "https://example.com/chicken.jpg",
                "g",
                100,
                165,
                31,
                3.6,
                0,
                0,
                0,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                85,
                "A"),
        };
        var aiItems = new List<ConsumptionAiItemModel> {
            new(aiItemId, sessionId, "Rice", "Ð Ð¸Ñ", 200, "g", 260, 5, 0.6, 56, 0.4, 0),
        };
        var sessions = new List<ConsumptionAiSessionModel> {
            new(sessionId, consumptionId, null, null, "Text", DateTime.UtcNow, "Notes", aiItems),
        };

        var model = new ConsumptionModel(
            consumptionId, DateTime.UtcNow, MealType: null, Comment: null, ImageUrl: null, ImageAssetId: null,
            425, 36, 4.2, 56, 0.4, 0, IsNutritionAutoCalculated: true, ManualCalories: null, ManualProteins: null, ManualFats: null, ManualCarbs: null, ManualFiber: null, ManualAlcohol: null,
            0, 0, 61, "yellow", IsFavorite: false, FavoriteMealId: null, items, sessions);

        ConsumptionHttpResponse response = model.ToHttpResponse();

        Assert.Single(response.Items);
        Assert.Equal(itemId, response.Items[0].Id);
        Assert.Equal(productId, response.Items[0].ProductId);
        Assert.Equal("Chicken", response.Items[0].ProductName);
        Assert.Equal("https://example.com/chicken.jpg", response.Items[0].ProductImageUrl);
        Assert.Equal(85, response.Items[0].ProductQualityScore);
        Assert.Equal("A", response.Items[0].ProductQualityGrade);

        Assert.Single(response.AiSessions);
        Assert.Equal(sessionId, response.AiSessions[0].Id);
        Assert.Single(response.AiSessions[0].Items);
        Assert.Equal("Rice", response.AiSessions[0].Items[0].NameEn);
        Assert.Equal("Ð Ð¸Ñ", response.AiSessions[0].Items[0].NameLocal);
        Assert.Equal(260, response.AiSessions[0].Items[0].Calories);
    }

    [Fact]
    public void ConsumptionModel_ToHttpResponse_WithManualNutrition() {
        var model = new ConsumptionModel(
            Guid.NewGuid(), DateTime.UtcNow, MealType: null, Comment: null, ImageUrl: null, ImageAssetId: null,
            0, 0, 0, 0, 0, 0, IsNutritionAutoCalculated: false, 500, 30, 20, 60, 5, 0,
            0, 0, 55, "yellow", IsFavorite: false, FavoriteMealId: null, [], []);

        ConsumptionHttpResponse response = model.ToHttpResponse();

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
            ImageUrl: null,
            ImageAssetId: null,
            500,
            30,
            20,
            60,
            5,
            0,
            IsNutritionAutoCalculated: true,
            ManualCalories: null,
            ManualProteins: null,
            ManualFats: null,
            ManualCarbs: null,
            ManualFiber: null,
            ManualAlcohol: null,
            3,
            7,
            70,
            "green",
            IsFavorite: true,
            favoriteMealId,
            [],
            []);

        ConsumptionHttpResponse response = model.ToHttpResponse();

        Assert.True(response.IsFavorite);
        Assert.Equal(favoriteMealId, response.FavoriteMealId);
    }

    [Fact]
    public void ConsumptionOverviewModel_ToHttpResponse_MapsNestedCollections() {
        var consumption = new ConsumptionModel(
            Guid.NewGuid(),
            DateTime.UtcNow,
            "Breakfast",
            Comment: null,
            ImageUrl: null,
            ImageAssetId: null,
            350,
            20,
            12,
            30,
            4,
            0,
            IsNutritionAutoCalculated: true,
            ManualCalories: null,
            ManualProteins: null,
            ManualFats: null,
            ManualCarbs: null,
            ManualFiber: null,
            ManualAlcohol: null,
            2,
            6,
            66,
            "yellow",
            IsFavorite: false,
            FavoriteMealId: null,
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

        ConsumptionOverviewHttpResponse response = overview.ToHttpResponse();

        Assert.Single(response.AllConsumptions.Data);
        Assert.Single(response.FavoriteItems);
        Assert.Equal(1, response.FavoriteTotalCount);
        Assert.Equal(consumption.Id, response.AllConsumptions.Data[0].Id);
        Assert.Equal(favorite.Id, response.FavoriteItems[0].Id);
    }
}
