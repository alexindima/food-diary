using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Modules.Ai.Infrastructure.Providers.Options;
using FoodDiary.Modules.Ai.Infrastructure.Providers.Services.OpenAi;
using FoodDiary.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FoodDiary.Modules.Ai.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
[Collection("OpenAI provider")]
public sealed class ProviderRecognitionContractTests {
    private const string Vision = """{"items":[{"nameEn":"Apple","nameLocal":null,"amount":0.125,"unit":"pcs","confidence":1}]}""";
    private const string Nutrition = """{"calories":52.125,"protein":0,"fat":0,"carbs":0,"fiber":0,"alcohol":0,"items":[{"name":"Apple","amount":0.125,"unit":"pcs","calories":52.125,"protein":0,"fat":0,"carbs":0,"fiber":0,"alcohol":0}]}""";

    public static TheoryData<string, string> InvalidValues() {
        var cases = new TheoryData<string, string>();
        foreach (string operation in new[] { "vision", "text" }) {
            foreach ((string field, decimal value) in new (string, decimal)[] {
                ("amount", 0), ("amount", -1), ("confidence", -0.01m), ("confidence", 1.01m),
                ("centerX", -0.01m), ("centerX", 1.01m), ("centerY", -0.01m), ("centerY", 1.01m),
                ("locationConfidence", -0.01m), ("locationConfidence", 1.01m),
            }) {
                JsonNode payload = JsonNode.Parse(Vision)!;
                payload["items"]![0]![field] = value;
                cases.Add(operation, payload.ToJsonString());
            }
        }
        foreach (string field in new[] { "calories", "protein", "fat", "carbs", "fiber", "alcohol" }) {
            JsonNode total = JsonNode.Parse(Nutrition)!;
            total[field] = -0.01m;
            cases.Add("nutrition", total.ToJsonString());
            JsonNode item = JsonNode.Parse(Nutrition)!;
            item["items"]![0]![field] = -0.01m;
            cases.Add("nutrition", item.ToJsonString());
        }
        foreach (decimal amount in new[] { 0m, -1m }) {
            JsonNode payload = JsonNode.Parse(Nutrition)!;
            payload["items"]![0]!["amount"] = amount;
            cases.Add("nutrition", payload.ToJsonString());
        }
        JsonNode empty = JsonNode.Parse(Nutrition)!;
        empty["items"] = new JsonArray();
        cases.Add("nutrition", empty.ToJsonString());
        JsonNode extra = JsonNode.Parse(Nutrition)!;
        extra["items"]!.AsArray().Add(extra["items"]![0]!.DeepClone());
        cases.Add("nutrition", extra.ToJsonString());
        return cases;
    }

    [Theory]
    [MemberData(nameof(InvalidValues))]
    public async Task InvalidProviderValues_AreRejectedWithoutFallbackOrAnotherPaidCall(string operation, string payload) {
        using var transport = new ResponseHandler(payload);
        using var http = new HttpClient(transport);
        OpenAiFoodClient client = Client(http);
        Result result = operation switch {
            "vision" => await client.AnalyzeFoodImageAsync("data:image/png;base64,AA==", "en", description: null, "prompt", CancellationToken.None),
            "text" => await client.ParseFoodTextAsync("apple", "en", "prompt", CancellationToken.None),
            _ => await client.CalculateNutritionAsync([Item()], "prompt", CancellationToken.None),
        };
        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("Ai.InvalidResponse", result.Error.Code),
            () => Assert.Equal(1, transport.Requests));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task ValidVision_RetainsFractionalQuantityUnitAndProbabilityBoundaries(int probability) {
        JsonNode payload = JsonNode.Parse(Vision)!;
        payload["items"]![0]!["confidence"] = probability;
        payload["items"]![0]!["centerX"] = 0;
        payload["items"]![0]!["centerY"] = 1;
        payload["items"]![0]!["locationConfidence"] = null;
        using var http = new HttpClient(new ResponseHandler(payload.ToJsonString()));
        Result<OpenAiFoodClientResponse<FoodVisionModel>> result = await Client(http).ParseFoodTextAsync("apple", "en", "prompt", CancellationToken.None);
        Assert.True(result.IsSuccess, result.Error.Message);
        FoodVisionItemModel item = Assert.Single(result.Value.Value.Items);
        Assert.Multiple(
            () => Assert.Equal(0.125m, item.Amount),
            () => Assert.Equal("pcs", item.Unit),
            () => Assert.Equal(probability, item.Confidence),
            () => Assert.Equal(0, item.CenterX),
            () => Assert.Equal(1, item.CenterY),
            () => Assert.Null(item.LocationConfidence));
    }

    [Fact]
    public async Task Nutrition_RetainsZerosPrecisionAndProviderTotalsWithoutRecalculatingFromMacros() {
        using var http = new HttpClient(new ResponseHandler(Nutrition));
        Result<OpenAiFoodClientResponse<FoodNutritionModel>> result = await Client(http).CalculateNutritionAsync([Item()], "prompt", CancellationToken.None);
        Assert.True(result.IsSuccess, result.Error.Message);
        FoodNutritionModel nutrition = result.Value.Value;
        FoodNutritionItemModel item = Assert.Single(nutrition.Items);
        Assert.Multiple(
            () => Assert.Equal(52.125m, nutrition.Calories),
            () => Assert.Equal(0, nutrition.Protein),
            () => Assert.Equal(0, nutrition.Fat),
            () => Assert.Equal(0, nutrition.Carbs),
            () => Assert.Equal(0, nutrition.Fiber),
            () => Assert.Equal(0, nutrition.Alcohol),
            () => Assert.Equal(52.125m, item.Calories),
            () => Assert.Equal(0.125m, item.Amount),
            () => Assert.Equal("pcs", item.Unit));
    }

    private static FoodVisionItemModel Item() => new("Apple", NameLocal: null, 0.125m, "pcs", 1);
    private static OpenAiFoodClient Client(HttpClient http) => new(http,
        Options.Create(new OpenAiOptions { ApiKey = "synthetic-test-key", VisionModel = "primary", VisionFallbackModel = "fallback", TextModel = "text" }),
        NullLogger<OpenAiFoodClient>.Instance);

    [ExcludeFromCodeCoverage]
    private sealed class ResponseHandler(string payload) : HttpMessageHandler {
        public int Requests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            Requests++;
            string body = JsonSerializer.Serialize(new { output = new[] { new { content = new[] { new { type = "output_text", text = payload } } } } });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        }
    }
}
