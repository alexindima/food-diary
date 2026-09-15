using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Lessons.Application.Models;

namespace FoodDiary.Modules.Lessons.Application.Queries.GetLessonById;

public record GetLessonByIdQuery(
    Guid? UserId,
    Guid LessonId) : IQuery<Result<LessonDetailModel>>, IUserRequest;
