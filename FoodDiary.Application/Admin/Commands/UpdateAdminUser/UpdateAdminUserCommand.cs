using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Admin;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Admin.Commands.UpdateAdminUser;

public sealed record UpdateAdminUserCommand(
    UserId UserId,
    bool? IsActive,
    bool? IsEmailConfirmed,
    string[] Roles,
    long? AiInputTokenLimit,
    long? AiOutputTokenLimit)
    : ICommand<Result<AdminUserResponse>>;
