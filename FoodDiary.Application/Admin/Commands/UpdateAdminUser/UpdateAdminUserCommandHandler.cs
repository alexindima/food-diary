using FoodDiary.Application.Admin.Mappings;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
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
        var userId = new UserId(command.UserId);
        var user = await userRepository.GetByIdIncludingDeletedAsync(userId, cancellationToken);
        if (user is null) {
            return Result.Failure<AdminUserModel>(Errors.User.NotFound(command.UserId));
        }

        if (command.IsActive.HasValue) {
            if (command.IsActive.Value) {
                user.Activate();
            } else {
                user.Deactivate();
            }
        }

        if (command.IsEmailConfirmed.HasValue) {
            user.SetEmailConfirmed(command.IsEmailConfirmed.Value);
        }

        var languageResult = NormalizeLanguage(command.Language);
        if (languageResult.IsFailure) {
            return Result.Failure<AdminUserModel>(languageResult.Error);
        }

        if (languageResult.Value is not null) {
            user.UpdatePreferences(language: languageResult.Value);
        }

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

            var roleEntities = await userRepository.GetRolesByNamesAsync(requestedRoles, cancellationToken);
            if (roleEntities.Count != requestedRoles.Length) {
                return Result.Failure<AdminUserModel>(
                    Errors.Validation.Invalid("roles", "One or more roles are not configured in the system."));
            }

            UpdateUserRoles(user, roleEntities);
        }

        if (command.AiInputTokenLimit.HasValue || command.AiOutputTokenLimit.HasValue) {
            if (command.AiInputTokenLimit is < 0) {
                return Result.Failure<AdminUserModel>(
                    Errors.Validation.Invalid("aiInputTokenLimit", "AI input token limit must be non-negative."));
            }

            if (command.AiOutputTokenLimit is < 0) {
                return Result.Failure<AdminUserModel>(
                    Errors.Validation.Invalid("aiOutputTokenLimit", "AI output token limit must be non-negative."));
            }

            user.UpdateAiTokenLimits(command.AiInputTokenLimit, command.AiOutputTokenLimit);
        }

        await userRepository.UpdateAsync(user, cancellationToken);
        return Result.Success(user.ToAdminModel());
    }

    private static void UpdateUserRoles(User user, IReadOnlyList<Role> roles) {
        user.ReplaceRoles(roles);
    }

    private static Result<string?> NormalizeLanguage(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return Result.Success<string?>(null);
        }

        return LanguageCode.TryParse(value, out var language)
            ? Result.Success<string?>(language.Value)
            : Result.Failure<string?>(Errors.Validation.Invalid("language", "Invalid language value."));
    }
}
