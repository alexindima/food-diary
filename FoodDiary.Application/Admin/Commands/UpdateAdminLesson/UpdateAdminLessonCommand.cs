using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;

namespace FoodDiary.Application.Admin.Commands.UpdateAdminLesson;

public sealed record UpdateAdminLessonCommand(
    Guid Id,
    string Title,
    string Content,
    string? Summary,
    string Locale,
    string Category,
    string Difficulty,
    int EstimatedReadMinutes,
    int SortOrder) : ICommand<Result<AdminLessonModel>>;
