using System.Diagnostics.CodeAnalysis;

namespace FoodDiary.Modules.Lessons.Contracts.Models;

[ExcludeFromCodeCoverage]
public sealed record LessonAdminReadModel(
    Guid Id,
    string Title,
    string Content,
    string? Summary,
    string Locale,
    string Category,
    string Difficulty,
    int EstimatedReadMinutes,
    int SortOrder,
    DateTime CreatedOnUtc,
    DateTime? ModifiedOnUtc,
    bool IsPublished = true,
    int CompletedCount = 0);
