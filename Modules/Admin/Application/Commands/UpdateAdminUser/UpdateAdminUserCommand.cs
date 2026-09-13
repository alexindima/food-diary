using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Admin.Application.Models;

namespace FoodDiary.Modules.Admin.Application.Commands.UpdateAdminUser;

public sealed record UpdateAdminUserCommand(
    Guid UserId,
    bool? IsActive,
    bool? IsEmailConfirmed,
    IReadOnlyList<string>? Roles,
    string? Language,
    long? AiInputTokenLimit,
    long? AiOutputTokenLimit,
    Guid? ActorUserId = null)
    : ICommand<Result<AdminUserModel>>;
