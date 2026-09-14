using System.Diagnostics.CodeAnalysis;

namespace FoodDiary.Modules.Ai.Contracts.Models;

[ExcludeFromCodeCoverage]
public sealed record AiPromptTemplateReadModel(
    Guid Id,
    string Key,
    string Locale,
    string PromptText,
    int Version,
    bool IsActive,
    DateTime CreatedOnUtc,
    DateTime? ModifiedOnUtc);
