using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Commands.ImportAdminLessons;

public sealed record ImportAdminLessonsCommand(
    int Version,
    IReadOnlyList<ImportAdminLessonItem> Lessons) : ICommand<Result<AdminLessonsImportModel>>;
