using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Commands.DeleteAdminLesson;

public sealed record DeleteAdminLessonCommand(Guid Id) : ICommand<Result>;
