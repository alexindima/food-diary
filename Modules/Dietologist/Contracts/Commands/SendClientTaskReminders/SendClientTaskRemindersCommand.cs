using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;

namespace FoodDiary.Modules.Dietologist.Contracts.Commands.SendClientTaskReminders;

public sealed record SendClientTaskRemindersCommand : ICommand<int>;
