using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Commands.DeleteAdminDailyAdviceGroup;

public sealed record DeleteAdminDailyAdviceGroupCommand(Guid Id) : ICommand<Result>;
