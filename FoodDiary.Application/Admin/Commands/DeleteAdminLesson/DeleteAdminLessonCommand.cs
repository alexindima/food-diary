using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;

namespace FoodDiary.Application.Admin.Commands.DeleteAdminLesson;

public sealed record DeleteAdminLessonCommand(Guid Id) : ICommand<Result>;
