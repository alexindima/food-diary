namespace FoodDiary.Modules.Identity.Presentation.Features.Auth.Responses;

public sealed record TelegramDiaryGoalsHttpResponse(double? Calories, double? Protein, double? Fat, double? Carbs, double? Fiber,
    double? Water, int? Steps, double? Weight, double? Waist);
