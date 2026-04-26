using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;

namespace FoodDiary.Application.Dashboard.Commands.SendDashboardTestEmail;

public sealed record SendDashboardTestEmailCommand(Guid UserId) : ICommand<Result>;
