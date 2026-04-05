using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;

namespace FoodDiary.Application.Lessons.Commands.MarkLessonRead;

public record MarkLessonReadCommand(
    Guid? UserId,
    Guid LessonId) : ICommand<Result>, IUserRequest;
