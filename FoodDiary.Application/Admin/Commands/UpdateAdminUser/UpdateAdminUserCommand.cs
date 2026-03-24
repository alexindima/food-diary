using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Admin.Models;

namespace FoodDiary.Application.Admin.Commands.UpdateAdminUser;

public sealed record UpdateAdminUserCommand(
    Guid UserId,
    bool? IsActive,
    bool? IsEmailConfirmed,
    IReadOnlyList<string>? Roles,
    string? Language,
    long? AiInputTokenLimit,
    long? AiOutputTokenLimit)
    : ICommand<Result<AdminUserModel>>;
