namespace FoodDiary.Modules.Identity.Presentation.Features.Auth.Responses;

public sealed record TelegramDiaryCountsHttpResponse(int Meals, int Recipes, int Products, int WaterEntries, int Measurements);
