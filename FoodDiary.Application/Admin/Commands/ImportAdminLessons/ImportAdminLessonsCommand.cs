using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;

namespace FoodDiary.Application.Admin.Commands.ImportAdminLessons;

public sealed record ImportAdminLessonsCommand(
    int Version,
    IReadOnlyList<ImportAdminLessonItem> Lessons) : ICommand<Result<AdminLessonsImportModel>>;

public sealed record ImportAdminLessonItem(
    string Title,
    string Content,
    string? Summary,
    string Locale,
    string Category,
    string Difficulty,
    int EstimatedReadMinutes,
    int SortOrder);
