using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Commands.CreateAdminUser;

public sealed record CreateAdminUserCommand(
    string Email,
    string? FirstName,
    string? LastName,
    string? Language,
    IReadOnlyList<string> Roles,
    string? TemporaryPassword,
    bool GeneratePassword,
    bool IsEmailConfirmed,
    bool SendCredentialsEmail,
    bool RequirePasswordChange,
    string? ClientOrigin,
    Guid ActorUserId)
    : ICommand<Result<AdminUserCreationModel>>;
