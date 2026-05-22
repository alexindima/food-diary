using FoodDiary.Application.Admin.Mappings;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Audit;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Admin.Commands.UpdateAdminUser;

public sealed class UpdateAdminUserCommandHandler(
    IUserRepository userRepository,
    IAuditLogger auditLogger,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<UpdateAdminUserCommand, Result<AdminUserModel>> {
    private const string RoleAuditSource = "AdminUserEditor";

    private static readonly HashSet<string> AllowedRoles = new(
        [RoleNames.Owner, RoleNames.Admin, RoleNames.Premium, RoleNames.Support],
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
        IReadOnlyList<UserRoleAuditEvent> roleAuditEvents = [];
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

            var isOwner = user.HasRole(RoleNames.Owner);
            var requestsOwner = requestedRoles.Contains(RoleNames.Owner, StringComparer.Ordinal);
            var requestsAdmin = requestedRoles.Contains(RoleNames.Admin, StringComparer.Ordinal);

            if (!isOwner && requestsOwner) {
                return Result.Failure<AdminUserModel>(
                    Errors.Validation.Invalid("roles", "Owner role cannot be assigned from the admin user editor."));
            }

            if (isOwner && (!requestsOwner || !requestsAdmin)) {
                return Result.Failure<AdminUserModel>(
                    Errors.Validation.Invalid("roles", "Owner users must keep Owner and Admin roles."));
            }

            roleEntities = await userRepository.GetRolesByNamesAsync(requestedRoles, cancellationToken);
            if (roleEntities.Count != requestedRoles.Length) {
                return Result.Failure<AdminUserModel>(
                    Errors.Validation.Invalid("roles", "One or more roles are not configured in the system."));
            }

            roleAuditEvents = CreateRoleAuditEvents(
                user,
                roleEntities,
                command.ActorUserId.HasValue ? new UserId(command.ActorUserId.Value) : null,
                dateTimeProvider.UtcNow);
        }

        if (command.IsActive.HasValue) {
            if (user.DeletedAt is not null) {
                return Result.Failure<AdminUserModel>(
                    Errors.Validation.Invalid(
                        nameof(command.IsActive),
                        "Deleted user lifecycle cannot be changed via admin active toggle. Use restore flow first."));
            }

            if (command.IsActive.Value) {
                user.Activate();
            } else {
                if (user.HasRole(RoleNames.Owner)) {
                    return Result.Failure<AdminUserModel>(
                        Errors.Validation.Invalid(nameof(command.IsActive), "Owner user cannot be deactivated."));
                }

                user.Deactivate();
            }
        }

        user.UpdateAdminSecurity(new UserAdminSecurityUpdate(command.IsEmailConfirmed));
        user.UpdateAdminPreferences(new UserAdminPreferenceUpdate(languageResult.Value));
        user.UpdateAdminAiQuota(new UserAdminAiQuotaUpdate(
            command.AiInputTokenLimit,
            command.AiOutputTokenLimit));

        if (roleEntities is not null) {
            user.ReplaceRoles(roleEntities);
        }

        await userRepository.UpdateAsync(user, roleAuditEvents, cancellationToken);

        auditLogger.Log(
            "admin.user.update",
            new UserId(command.UserId),
            "User",
            command.UserId.ToString(),
            $"roles={command.Roles?.Count.ToString() ?? "unchanged"} isActive={command.IsActive?.ToString() ?? "unchanged"}");

        return Result.Success(user.ToAdminModel());
    }

    private static IReadOnlyList<UserRoleAuditEvent> CreateRoleAuditEvents(
        User user,
        IReadOnlyCollection<Role> requestedRoles,
        UserId? actorUserId,
        DateTime occurredAtUtc) {
        var currentRolesByName = user.UserRoles
            .Select(userRole => userRole.Role)
            .ToDictionary(role => role.Name, StringComparer.Ordinal);
        var requestedRolesByName = requestedRoles
            .ToDictionary(role => role.Name, StringComparer.Ordinal);

        var addedEvents = requestedRolesByName
            .Where(item => !currentRolesByName.ContainsKey(item.Key))
            .Select(item => UserRoleAuditEvent.Create(
                user.Id,
                item.Value,
                UserRoleAuditAction.Added,
                actorUserId,
                RoleAuditSource,
                occurredAtUtc));

        var removedEvents = currentRolesByName
            .Where(item => !requestedRolesByName.ContainsKey(item.Key))
            .Select(item => UserRoleAuditEvent.Create(
                user.Id,
                item.Value,
                UserRoleAuditAction.Removed,
                actorUserId,
                RoleAuditSource,
                occurredAtUtc));

        return addedEvents
            .Concat(removedEvents)
            .OrderBy(auditEvent => auditEvent.RoleName, StringComparer.Ordinal)
            .ThenBy(auditEvent => auditEvent.Action)
            .ToArray();
    }
}
