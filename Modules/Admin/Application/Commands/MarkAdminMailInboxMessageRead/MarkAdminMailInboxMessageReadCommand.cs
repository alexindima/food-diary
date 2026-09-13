using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;

namespace FoodDiary.Modules.Admin.Application.Commands.MarkAdminMailInboxMessageRead;

public sealed record MarkAdminMailInboxMessageReadCommand(Guid Id) : ICommand<Result>;
