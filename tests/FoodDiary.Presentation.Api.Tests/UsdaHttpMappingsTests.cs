using FoodDiary.Application.Usda.Models;
using FoodDiary.Presentation.Api.Features.Usda.Mappings;
using FoodDiary.Presentation.Api.Features.Usda.Requests;

namespace FoodDiary.Presentation.Api.Tests;

public sealed class UsdaHttpMappingsTests {
    [Fact]
    public void ToQuery_Search_MapsSearchAndLimit() {
        var query = UsdaHttpMappings.ToQuery("chicken", 10);

        Assert.Equal("chicken", query.Search);
        Assert.Equal(10, query.Limit);
    }

    [Fact]
    public void ToQuery_Micronutrients_MapsFdcId() {
        var query = UsdaHttpMappings.ToQuery(12345);

        Assert.Equal(12345, query.FdcId);
    }

    [Fact]
    public void LinkRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var request = new LinkProductToUsdaFoodHttpRequest(54321);

        var command = request.ToCommand(userId, productId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(productId, command.ProductId);
        Assert.Equal(54321, command.FdcId);
    }

    [Fact]
    public void ToUnlinkCommand_MapsUserIdAndProductId() {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var command = UsdaHttpMappings.ToUnlinkCommand(userId, productId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(productId, command.ProductId);
    }

    [Fact]
    public void ToDailyQuery_MapsUserIdAndDate() {
        var userId = Guid.NewGuid();
        var date = new DateTime(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc);

        var query = UsdaHttpMappings.ToDailyQuery(userId, date);

        Assert.Equal(userId, query.UserId);
        Assert.Equal(date, query.Date);
    }

    [Fact]
    public void UsdaFoodModel_ToHttpResponse_MapsAllFields() {
        var model = new UsdaFoodModel(12345, "Chicken breast", "Poultry");

        var response = model.ToHttpResponse();

        Assert.Equal(12345, response.FdcId);
        Assert.Equal("Chicken breast", response.Description);
        Assert.Equal("Poultry", response.FoodCategory);
    }

    [Fact]
    public void UsdaFoodModelList_ToHttpResponse_MapsAll() {
        var models = new List<UsdaFoodModel> {
            new(1, "Apple", "Fruit"),
            new(2, "Banana", null),
        };

        var responses = models.ToHttpResponse();

        Assert.Equal(2, responses.Count);
        Assert.Equal("Apple", responses[0].Description);
        Assert.Null(responses[1].FoodCategory);
    }

    [Fact]
    public void UsdaFoodDetailModel_ToHttpResponse_MapsNestedFields() {
        var nutrients = new List<MicronutrientModel> {
            new(1, "Vitamin C", "mg", 50, 90, 55.5),
        };
        var portions = new List<UsdaFoodPortionModel> {
            new(1, 1.0, "cup", 150, "1 cup chopped", null),
        };
        var scores = new HealthAreaScoresModel(
            new HealthAreaScoreModel(85, "A"),
            new HealthAreaScoreModel(70, "B"),
            new HealthAreaScoreModel(90, "A"),
            new HealthAreaScoreModel(60, "C"),
            new HealthAreaScoreModel(75, "B"));
        var model = new UsdaFoodDetailModel(123, "Broccoli", "Vegetable", nutrients, portions, scores);

        var response = model.ToHttpResponse();

        Assert.Equal(123, response.FdcId);
        Assert.Single(response.Nutrients);
        Assert.Equal("Vitamin C", response.Nutrients[0].Name);
        Assert.Single(response.Portions);
        Assert.Equal(150, response.Portions[0].GramWeight);
        Assert.NotNull(response.HealthScores);
        Assert.Equal(85, response.HealthScores.Heart.Score);
    }

    [Fact]
    public void DailyMicronutrientSummaryModel_ToHttpResponse_MapsAllFields() {
        var date = new DateTime(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc);
        var nutrients = new List<DailyMicronutrientModel> {
            new(1, "Iron", "mg", 12.5, 18, 69.4),
        };
        var model = new DailyMicronutrientSummaryModel(date, 5, 8, nutrients, null);

        var response = model.ToHttpResponse();

        Assert.Equal(date, response.Date);
        Assert.Equal(5, response.LinkedProductCount);
        Assert.Equal(8, response.TotalProductCount);
        Assert.Single(response.Nutrients);
        Assert.Null(response.HealthScores);
    }
}
