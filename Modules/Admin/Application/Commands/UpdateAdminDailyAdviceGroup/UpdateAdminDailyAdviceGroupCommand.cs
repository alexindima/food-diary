using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Commands.UpdateAdminDailyAdviceGroup;

public sealed record UpdateAdminDailyAdviceGroupCommand(Guid Id, string Ru, string En, int Weight, string? Tag) : ICommand<Result<AdminDailyAdviceGroupModel>>;
