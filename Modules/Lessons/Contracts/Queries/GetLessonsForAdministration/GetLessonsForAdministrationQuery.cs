using FoodDiary.Modules.Lessons.Contracts.Models;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Lessons.Contracts.Queries.GetLessonsForAdministration;

public sealed record GetLessonsForAdministrationQuery(int Page, int Limit) : IRequest<IReadOnlyList<LessonAdminReadModel>>;
