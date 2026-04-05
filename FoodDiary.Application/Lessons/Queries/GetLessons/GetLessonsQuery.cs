using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Lessons.Models;

namespace FoodDiary.Application.Lessons.Queries.GetLessons;

public record GetLessonsQuery(
    Guid? UserId,
    string Locale,
    string? Category) : IQuery<Result<IReadOnlyList<LessonSummaryModel>>>, IUserRequest;
