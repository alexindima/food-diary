using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Commands.ImportAdminLessons;

public sealed record ImportAdminLessonsCommand(
    int Version,
    IReadOnlyList<ImportAdminLessonItem> Lessons) : ICommand<Result<AdminLessonsImportModel>>;
