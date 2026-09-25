namespace FoodDiary.Modules.Ai.Contracts.Models;

public sealed record FoodVisionModel(
    IReadOnlyList<FoodVisionItemModel> Items,
    string? Notes = null,
    ProductLabelModel? ProductLabel = null);
