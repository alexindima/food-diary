using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Commands.DeleteAdminLesson;

public sealed record DeleteAdminLessonCommand(Guid Id) : ICommand<Result>;
