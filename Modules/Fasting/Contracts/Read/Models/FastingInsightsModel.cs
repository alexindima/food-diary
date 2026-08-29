namespace FoodDiary.Modules.Fasting.Contracts.Read.Models;

public sealed record FastingInsightsModel(
    IReadOnlyList<FastingMessageModel> Alerts,
    IReadOnlyList<FastingMessageModel> Insights);
