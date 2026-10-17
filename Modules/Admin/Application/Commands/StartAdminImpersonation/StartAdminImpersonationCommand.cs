using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Results;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;

namespace FoodDiary.Modules.Admin.Application.Commands.StartAdminImpersonation;

public sealed record StartAdminImpersonationCommand(
    Guid ActorUserId,
    Guid TargetUserId,
    string Reason,
    string? ActorIpAddress,
    string? ActorUserAgent,
    Guid ActorSessionId)
    : ICommand<Result<AdminImpersonationStartModel>>;
