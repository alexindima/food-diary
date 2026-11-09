namespace FoodDiary.Modules.Users.Contracts.Common;

public sealed record TelegramDiaryGoals(double? Calories, double? Protein, double? Fat, double? Carbs, double? Fiber,
    double? Water, int? Steps, double? Weight, double? Waist);
