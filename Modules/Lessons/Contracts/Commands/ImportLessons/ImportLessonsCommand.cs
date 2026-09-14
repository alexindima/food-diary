using FoodDiary.Modules.Lessons.Contracts.Common;
using FoodDiary.Modules.Lessons.Contracts.Models;
using FoodDiary.Results;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Lessons.Contracts.Commands.ImportLessons;

// Executes within the caller-owned unit of work; this request does not commit.
public sealed record ImportLessonsCommand(
    IReadOnlyList<LessonAdministrationItem> Items) : IRequest<Result<IReadOnlyList<LessonAdminReadModel>>>;
