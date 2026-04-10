namespace FoodDiary.Application.Fasting.Models;

public sealed record FastingInsightsModel(
    IReadOnlyList<FastingMessageModel> Insights,
    FastingMessageModel? CurrentPrompt);
