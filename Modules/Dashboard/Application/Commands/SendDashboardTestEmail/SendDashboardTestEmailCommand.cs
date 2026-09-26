using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Dashboard.Application.Commands.SendDashboardTestEmail;

public sealed record SendDashboardTestEmailCommand(Guid UserId) : ICommand<Result>;
