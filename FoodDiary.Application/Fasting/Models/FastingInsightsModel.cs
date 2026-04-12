namespace FoodDiary.Application.Fasting.Models;

public sealed record FastingInsightsModel(
    IReadOnlyList<FastingMessageModel> Alerts,
    IReadOnlyList<FastingMessageModel> Insights);
