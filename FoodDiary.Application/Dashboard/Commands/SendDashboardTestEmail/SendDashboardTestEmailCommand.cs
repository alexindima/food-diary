using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Dashboard.Commands.SendDashboardTestEmail;

public sealed record SendDashboardTestEmailCommand(Guid UserId) : ICommand<Result>;
