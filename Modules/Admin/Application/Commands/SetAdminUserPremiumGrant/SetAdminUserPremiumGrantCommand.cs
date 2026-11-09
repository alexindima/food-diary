using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Commands.SetAdminUserPremiumGrant;

public sealed record SetAdminUserPremiumGrantCommand(Guid UserId, Guid ActorUserId, DateTimeOffset? EndsAtUtc, string? Reason)
    : ICommand<Result<UserManualPremiumGrantModel>>;
