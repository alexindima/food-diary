using FoodDiary.Application.Admin.Mappings;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Admin.Commands.UpdateAdminUser;

public sealed class UpdateAdminUserCommandHandler(IUserRepository userRepository)
    : ICommandHandler<UpdateAdminUserCommand, Result<AdminUserModel>> {
    private static readonly HashSet<string> AllowedRoles = new(
        [RoleNames.Admin, RoleNames.Premium, RoleNames.Support],
        StringComparer.Ordinal);

    public async Task<Result<AdminUserModel>> Handle(
        UpdateAdminUserCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId == Guid.Empty) {
            return Result.Failure<AdminUserModel>(
                Errors.Validation.Invalid(nameof(command.UserId), "User id must not be empty."));
        }

        var userId = new UserId(command.UserId);
        var user = await userRepository.GetByIdIncludingDeletedAsync(userId, cancellationToken);
        if (user is null) {
            return Result.Failure<AdminUserModel>(Errors.User.NotFound(command.UserId));
        }

        var languageResult = StringCodeParser.ParseOptionalLanguage(
            command.Language,
            "language",
            "Invalid language value.");
        if (languageResult.IsFailure) {
            return Result.Failure<AdminUserModel>(languageResult.Error);
        }

        IReadOnlyList<Role>? roleEntities = null;
        if (command.Roles is not null) {
            var requestedRoles = command.Roles
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Select(role => role.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (requestedRoles.Any(role => !AllowedRoles.Contains(role))) {
                return Result.Failure<AdminUserModel>(
                    Errors.Validation.Invalid("roles", "Unknown role."));
            }

            roleEntities = await userRepository.GetRolesByNamesAsync(requestedRoles, cancellationToken);
            if (roleEntities.Count != requestedRoles.Length) {
                return Result.Failure<AdminUserModel>(
                    Errors.Validation.Invalid("roles", "One or more roles are not configured in the system."));
            }
        }

        if (command.IsActive.HasValue) {
            if (command.IsActive.Value) {
                user.Activate();
            } else {
                user.Deactivate();
            }
        }

        user.UpdateAdminAccount(new UserAdminAccountUpdate(
            IsEmailConfirmed: command.IsEmailConfirmed,
            Language: languageResult.Value,
            AiInputTokenLimit: command.AiInputTokenLimit,
            AiOutputTokenLimit: command.AiOutputTokenLimit));

        if (roleEntities is not null) {
            user.ReplaceRoles(roleEntities);
        }

        await userRepository.UpdateAsync(user, cancellationToken);
        return Result.Success(user.ToAdminModel());
    }
}
