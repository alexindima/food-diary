using FoodDiary.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;

namespace FoodDiary.Application.Admin.Commands.MarkAdminMailInboxMessageRead;

public sealed record MarkAdminMailInboxMessageReadCommand(Guid Id) : ICommand<Result>;
