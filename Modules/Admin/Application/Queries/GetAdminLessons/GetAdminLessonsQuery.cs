using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminLessons;

public sealed record GetAdminLessonsQuery : IQuery<Result<IReadOnlyList<AdminLessonModel>>>;
