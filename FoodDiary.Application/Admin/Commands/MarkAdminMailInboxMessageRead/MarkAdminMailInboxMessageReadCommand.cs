using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;

namespace FoodDiary.Application.Admin.Commands.MarkAdminMailInboxMessageRead;

public sealed record MarkAdminMailInboxMessageReadCommand(Guid Id) : ICommand<Result>;
