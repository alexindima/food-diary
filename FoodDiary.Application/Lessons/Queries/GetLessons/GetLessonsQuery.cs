using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Lessons.Models;

namespace FoodDiary.Application.Lessons.Queries.GetLessons;

public record GetLessonsQuery(
    Guid? UserId,
    string Locale,
    string? Category = null,
    string? Difficulty = null,
    string? Search = null,
    string? Sort = null,
    int Page = 1,
    int PageSize = 20) : IQuery<Result<LessonPageModel>>, IUserRequest;
