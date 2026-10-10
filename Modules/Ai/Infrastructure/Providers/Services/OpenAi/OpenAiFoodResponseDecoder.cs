using System.Text.Json;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Integrations.Http.Http;
using FoodDiary.Results;

namespace FoodDiary.Modules.Ai.Infrastructure.Providers.Services.OpenAi;

/// <summary>Translates untrusted provider output into the owning application contract.</summary>
internal static class OpenAiFoodResponseDecoder {
    private static readonly JsonSerializerOptions Options = new() {
        PropertyNameCaseInsensitive = true,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        MaxDepth = BoundedHttpContentReader.DefaultJsonMaxDepth,
    };

    public static Result<FoodVisionModel> Vision(string? text) {
        if (string.IsNullOrWhiteSpace(text)) {
            return Result.Failure<FoodVisionModel>(AiErrors.InvalidResponse("Missing output text."));
        }
        try {
            FoodVisionModel? parsed = JsonSerializer.Deserialize<FoodVisionModel>(text, Options);
            if (parsed is null) {
                return Result.Failure<FoodVisionModel>(AiErrors.InvalidResponse("Vision response is empty."));
            }
            return FoodRecognitionPolicy.IsValidVision(parsed)
                ? Result.Success(parsed)
                : Result.Failure<FoodVisionModel>(AiErrors.InvalidResponse("Vision items are invalid."));
        } catch (JsonException) {
            return Result.Failure<FoodVisionModel>(AiErrors.InvalidResponse("Vision JSON invalid."));
        }
    }

    public static Result<FoodNutritionModel> Nutrition(string? text, int expectedItems) {
        if (string.IsNullOrWhiteSpace(text)) {
            return Result.Failure<FoodNutritionModel>(AiErrors.InvalidResponse("Missing output text."));
        }
        try {
            FoodNutritionModel? parsed = JsonSerializer.Deserialize<FoodNutritionModel>(text, Options);
            if (parsed is null) {
                return Result.Failure<FoodNutritionModel>(AiErrors.InvalidResponse("Nutrition response is empty."));
            }
            return FoodRecognitionPolicy.IsValidNutrition(parsed, expectedItems)
                ? Result.Success(parsed)
                : Result.Failure<FoodNutritionModel>(AiErrors.InvalidResponse("Nutrition items are invalid."));
        } catch (JsonException) {
            return Result.Failure<FoodNutritionModel>(AiErrors.InvalidResponse("Nutrition JSON invalid."));
        }
    }

    public static Result<FoodVisionModel> ProductLabel(string? text) {
        if (string.IsNullOrWhiteSpace(text)) {
            return Result.Failure<FoodVisionModel>(AiErrors.InvalidResponse("Missing product label output."));
        }
        try {
            ProductLabelModel? label = JsonSerializer.Deserialize<ProductLabelModel>(text, Options);
            if (label is null || label.BaseAmount is <= 0 || label.BaseUnit is not (null or "g" or "ml" or "pcs") ||
                new[] { label.Calories, label.Protein, label.Fat, label.Carbs, label.Fiber, label.Alcohol }.Any(value => value is < 0)) {
                return Result.Failure<FoodVisionModel>(AiErrors.InvalidResponse("Invalid product label values."));
            }
            return Result.Success(new FoodVisionModel([], ProductLabel: label));
        } catch (JsonException) {
            return Result.Failure<FoodVisionModel>(AiErrors.InvalidResponse("Invalid product label JSON."));
        }
    }
}
