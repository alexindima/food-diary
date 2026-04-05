using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Lessons.Models;

namespace FoodDiary.Application.Lessons.Queries.GetLessonById;

public record GetLessonByIdQuery(
    Guid? UserId,
    Guid LessonId) : IQuery<Result<LessonDetailModel>>, IUserRequest;
