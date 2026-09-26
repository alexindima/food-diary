using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Lessons.Application.Commands.MarkLessonRead;

public record MarkLessonReadCommand(
    Guid? UserId,
    Guid LessonId) : ICommand<Result>, IUserRequest;
