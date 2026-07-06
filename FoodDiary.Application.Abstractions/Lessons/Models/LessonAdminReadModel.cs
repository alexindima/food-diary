using System.Diagnostics.CodeAnalysis;

namespace FoodDiary.Application.Abstractions.Lessons.Models;

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
    DateTime? ModifiedOnUtc);
