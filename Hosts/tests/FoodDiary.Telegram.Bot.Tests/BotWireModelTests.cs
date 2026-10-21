using System.Text.Json;
using System.Text.Json.Nodes;
using FoodDiary.Telegram.Bot.Api.Generated.Model;

namespace FoodDiary.Telegram.Bot.Tests;

[ExcludeFromCodeCoverage]
public sealed class BotWireModelTests {
    public static TheoryData<Type, string> WireExamples => new() {
        { typeof(ApiErrorHttpResponse), """{"error":"Validation","errors":{"name":["Required"]},"message":"Invalid input","traceId":"trace-123"}""" },
        { typeof(ConfirmImageUploadHttpResponse), """{"assetId":"11111111-1111-1111-1111-111111111111","fileUrl":"https://storage.example/image"}""" },
        { typeof(CreateHydrationEntryHttpRequest), """{"amountMl":250,"timestampUtc":"2026-10-07T09:30:00Z"}""" },
        { typeof(DashboardLayoutHttpModel), """{"mobile":["water"],"setupHintDismissed":true,"web":["meals","water"]}""" },
        { typeof(FoodNutritionItemHttpResponse), NutritionItem },
        { typeof(FoodNutritionHttpResponse), $$"""{"alcohol":0,"calories":123.4567890123456789,"carbs":22.5,"fat":3.25,"fiber":2.5,"items":[{{NutritionItem}}],"notes":"Measured serving","protein":7.5}""" },
        { typeof(FoodRecognitionImageHttpResponse), """{"imageAssetId":"22222222-2222-2222-2222-222222222222","imageUrl":"https://storage.example/side"}""" },
        { typeof(FoodRecognitionJobHttpResponse), """
            {"additionalImages":[{"imageAssetId":"22222222-2222-2222-2222-222222222222","imageUrl":"https://storage.example/side"}],
             "createdOnUtc":"2026-10-07T09:30:00Z","description":"Lunch","errorCode":null,
             "id":"11111111-1111-1111-1111-111111111111","imageAssetId":"33333333-3333-3333-3333-333333333333",
             "imageUrl":"https://storage.example/main","isProductLabel":false,"nutrition":null,"nutritionErrorCode":null,
             "status":"Succeeded","updatedOnUtc":"2026-10-07T09:31:00Z","vision":{"items":[],"notes":"Ready","productLabel":null}}
            """ },
        { typeof(FoodVisionHttpResponse), $$"""{"items":[{{VisionItem}}],"notes":"Recognized label","productLabel":{{ProductLabel}}}""" },
        { typeof(FoodVisionItemHttpModel), VisionItem },
        { typeof(HydrationEntryHttpResponse), """{"amountMl":250,"id":"11111111-1111-1111-1111-111111111111","timestampUtc":"2026-10-07T09:30:00Z"}""" },
        { typeof(ProductLabelHttpResponse), ProductLabel },
        { typeof(StartFoodRecognitionHttpRequest), """{"additionalImageAssetIds":["22222222-2222-2222-2222-222222222222"],"description":"Lunch","id":"11111111-1111-1111-1111-111111111111","imageAssetId":"33333333-3333-3333-3333-333333333333","isProductLabel":true}""" },
        { typeof(UserHttpResponse), """
            {"activityLevel":"moderate","aiConsentAcceptedAt":"2026-10-07T09:30:00Z","birthDate":"1990-01-02T00:00:00Z",
             "carbTarget":200,"dailyCalorieTarget":2100,"dashboardLayout":{"mobile":["water"],"setupHintDismissed":false,"web":["meals"]},
             "desiredWaistCm":75,"desiredWeightKg":65,"email":"fixture@example.org","fastingCheckInFollowUpReminderHours":2,
             "fastingCheckInReminderHours":4,"fastingPushNotificationsEnabled":true,"fatTarget":70,"fiberTarget":25,"firstName":"Анна",
             "gender":"female","hasGoogleIdentity":true,"hasPassword":false,"hasTelegramIdentity":true,"heightCm":170,
             "hydrationGoal":2000,"id":"11111111-1111-1111-1111-111111111111","isActive":true,"isEmailConfirmed":false,
             "language":"ru","lastLoginAtUtc":"2026-10-07T09:30:00Z","lastName":"Fixture","mustChangePassword":false,
             "profileImage":"https://storage.example/profile","profileImageAssetId":"22222222-2222-2222-2222-222222222222",
             "proteinTarget":100,"pushNotificationsEnabled":true,"socialPushNotificationsEnabled":false,"stepGoal":8000,
             "surfaceStyle":"flat","theme":"dark","timeZoneId":"Asia/Tbilisi","uiStyle":"classic","username":"fixture",
             "waterGoal":2000,"weightKg":68.5}
            """ },
    };

    [Theory]
    [MemberData(nameof(WireExamples))]
    public void Models_PreserveExplicitWireNamesValuesAndNestedShapes(Type type, string json) {
        object? model = JsonSerializer.Deserialize(json, type);
        Assert.NotNull(model);
        Assert.IsType(type, model);

        string serialized = JsonSerializer.Serialize(model, type);

        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(json), JsonNode.Parse(serialized)),
            $"{type.Name} changed its wire shape. Expected: {json}; actual: {serialized}");
    }

    private const string NutritionItem = """{"alcohol":0,"amount":125.5,"calories":180.5,"carbs":22.5,"fat":3.25,"fiber":2.5,"name":"Oats","protein":7.5,"unit":"g"}""";
    private const string VisionItem = """{"amount":125.5,"centerX":0.25,"centerY":0.75,"confidence":0.95,"locationConfidence":0.8,"nameEn":"Oats","nameLocal":"Овсянка","unit":"g"}""";
    private const string ProductLabel = """{"alcohol":0,"baseAmount":100,"baseUnit":"g","brand":"Fixture","calories":389,"carbs":66,"fat":7,"fiber":10,"name":"Oats","notes":"Per 100 g","protein":17}""";
}
