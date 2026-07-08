using FoodDiary.Application.Admin.Models;
using FoodDiary.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;

namespace FoodDiary.Application.Admin.Commands.StartAdminImpersonation;

public sealed record StartAdminImpersonationCommand(
    Guid ActorUserId,
    Guid TargetUserId,
    string Reason,
    string? ActorIpAddress,
    string? ActorUserAgent)
    : ICommand<Result<AdminImpersonationStartModel>>;
