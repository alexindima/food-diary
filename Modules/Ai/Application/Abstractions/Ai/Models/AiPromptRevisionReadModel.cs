namespace FoodDiary.Application.Abstractions.Ai.Models;

public sealed record AiPromptRevisionReadModel(Guid Id, string PromptText, int Version, bool IsActive,
    DateTime SavedOnUtc, DateTime ArchivedOnUtc);
