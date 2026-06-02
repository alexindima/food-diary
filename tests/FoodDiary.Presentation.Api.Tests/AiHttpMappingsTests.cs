using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Presentation.Api.Features.Ai.Mappings;
using FoodDiary.Presentation.Api.Features.Ai.Models;
using FoodDiary.Presentation.Api.Features.Ai.Requests;

namespace FoodDiary.Presentation.Api.Tests;

public sealed class AiHttpMappingsTests {
    [Fact]
    public void UserId_ToUsageQuery_MapsUserId() {
        var userId = Guid.NewGuid();

        var query = userId.ToUsageQuery();

        Assert.Equal(userId, query.UserId);
    }

    [Fact]
    public void FoodVisionHttpRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var imageAssetId = Guid.NewGuid();
        var request = new FoodVisionHttpRequest(imageAssetId, "Dinner plate");

        var command = request.ToCommand(userId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(imageAssetId, command.ImageAssetId);
        Assert.Equal("Dinner plate", command.Description);
    }

    [Fact]
    public void FoodTextHttpRequest_ToCommand_MapsText() {
        var userId = Guid.NewGuid();
        var request = new FoodTextHttpRequest("two eggs and toast");

        var command = request.ToCommand(userId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(request.Text, command.Text);
    }

    [Fact]
    public void FoodNutritionHttpRequest_ToCommand_MapsNestedItems() {
        var userId = Guid.NewGuid();
        var request = new FoodNutritionHttpRequest([
            new FoodVisionItemHttpModel("egg", "яйцо", 2, "pcs", 0.95m)
        ]);

        var command = request.ToCommand(userId);

        Assert.Equal(userId, command.UserId);
        var item = Assert.Single(command.Items);
        Assert.Equal("egg", item.NameEn);
        Assert.Equal("яйцо", item.NameLocal);
        Assert.Equal(2, item.Amount);
        Assert.Equal("pcs", item.Unit);
        Assert.Equal(0.95m, item.Confidence);
    }

    [Fact]
    public void FoodVisionModel_ToHttpResponse_MapsNestedItems() {
        var model = new FoodVisionModel(
            Items: [new FoodVisionItemModel("egg", "яйцо", 2, "pcs", 0.95m)],
            Notes: "looks cooked");

        var response = model.ToHttpResponse();

        Assert.Equal("looks cooked", response.Notes);
        var item = Assert.Single(response.Items);
        Assert.Equal("egg", item.NameEn);
        Assert.Equal("яйцо", item.NameLocal);
        Assert.Equal(2, item.Amount);
        Assert.Equal("pcs", item.Unit);
        Assert.Equal(0.95m, item.Confidence);
    }

    [Fact]
    public void FoodNutritionModel_ToHttpResponse_MapsTotalsAndNestedItems() {
        var model = new FoodNutritionModel(
            Calories: 300,
            Protein: 20,
            Fat: 10,
            Carbs: 30,
            Fiber: 5,
            Alcohol: 0,
            Items: [new FoodNutritionItemModel("egg", 2, "pcs", 160, 12, 10, 1, 0, 0)],
            Notes: "manual check");

        var response = model.ToHttpResponse();

        Assert.Equal(300, response.Calories);
        Assert.Equal(20, response.Protein);
        Assert.Equal(10, response.Fat);
        Assert.Equal(30, response.Carbs);
        Assert.Equal(5, response.Fiber);
        Assert.Equal(0, response.Alcohol);
        Assert.Equal("manual check", response.Notes);
        var item = Assert.Single(response.Items);
        Assert.Equal("egg", item.Name);
        Assert.Equal(160, item.Calories);
        Assert.Equal(12, item.Protein);
    }

    [Fact]
    public void UserAiUsageModel_ToHttpResponse_MapsAllFields() {
        var resetAt = DateTime.UtcNow.AddHours(1);
        var model = new UserAiUsageModel(
            InputLimit: 1000,
            OutputLimit: 2000,
            InputUsed: 100,
            OutputUsed: 200,
            ResetAtUtc: resetAt);

        var response = model.ToHttpResponse();

        Assert.Equal(1000, response.InputLimit);
        Assert.Equal(2000, response.OutputLimit);
        Assert.Equal(100, response.InputUsed);
        Assert.Equal(200, response.OutputUsed);
        Assert.Equal(resetAt, response.ResetAtUtc);
    }
}
