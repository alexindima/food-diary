using FoodDiary.Application.Admin.Mappings;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Audit;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Admin.Commands.UpdateAdminUser;

public sealed class UpdateAdminUserCommandHandler(
    IAdminUserManagementService userManagementService,
    IAuditLogger auditLogger,
    TimeProvider dateTimeProvider)
    : ICommandHandler<UpdateAdminUserCommand, Result<AdminUserModel>> {
    private const string RoleAuditSource = "AdminUserEditor";

    private static readonly HashSet<string> AllowedRoles = new(
        [RoleNames.Owner, RoleNames.Admin, RoleNames.Premium, RoleNames.Support],
        StringComparer.Ordinal);

    private sealed record RoleUpdate(
        IReadOnlyList<Role> Roles,
        IReadOnlyList<UserRoleAuditEvent> AuditEvents);

    public async Task<Result<AdminUserModel>> Handle(
        UpdateAdminUserCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId == Guid.Empty) {
            return Result.Failure<AdminUserModel>(
                Errors.Validation.Invalid(nameof(command.UserId), "User id must not be empty."));
        }

        var userId = new UserId(command.UserId);
        User? user = await userManagementService.GetByIdIncludingDeletedAsync(userId, cancellationToken).ConfigureAwait(false);
        if (user is null) {
            return Result.Failure<AdminUserModel>(Errors.User.NotFound(command.UserId));
        }

        Result<string?> languageResult = StringCodeParser.ParseOptionalLanguage(
            command.Language,
            "language",
            "Invalid language value.");
        if (languageResult.IsFailure) {
            return Result.Failure<AdminUserModel>(languageResult.Error);
        }

        Result<RoleUpdate?> roleUpdateResult = await PrepareRoleUpdateAsync(user, command, cancellationToken).ConfigureAwait(false);
        if (roleUpdateResult.IsFailure) {
            return Result.Failure<AdminUserModel>(roleUpdateResult.Error);
        }

        Error? lifecycleError = ApplyLifecycleUpdate(user, command);
        if (lifecycleError is not null) {
            return Result.Failure<AdminUserModel>(lifecycleError);
        }

        user.UpdateAdminSecurity(new UserAdminSecurityUpdate(command.IsEmailConfirmed));
        user.UpdateAdminPreferences(new UserAdminPreferenceUpdate(languageResult.Value));
        user.UpdateAdminAiQuota(new UserAdminAiQuotaUpdate(
            command.AiInputTokenLimit,
            command.AiOutputTokenLimit));

        if (roleUpdateResult.Value is not null) {
            user.ReplaceRoles(roleUpdateResult.Value.Roles);
        }

        await userManagementService.UpdateAsync(
            user,
            roleUpdateResult.Value?.AuditEvents ?? [],
            cancellationToken).ConfigureAwait(false);

        auditLogger.Log(
            "admin.user.update",
            new UserId(command.UserId),
            "User",
            command.UserId.ToString(),
            $"roles={command.Roles?.Count.ToString() ?? "unchanged"} isActive={command.IsActive?.ToString() ?? "unchanged"}");

        return Result.Success(user.ToAdminModel());
    }

    private static bool IsSelfUpdate(UpdateAdminUserCommand command) =>
        command.ActorUserId == command.UserId;

    private async Task<Result<RoleUpdate?>> PrepareRoleUpdateAsync(
        User user,
        UpdateAdminUserCommand command,
        CancellationToken cancellationToken) {
        if (command.Roles is null) {
            return Result.Success<RoleUpdate?>(value: null);
        }

        string[] requestedRoles = [.. command.Roles
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => role.Trim())
            .Distinct(StringComparer.Ordinal)];

        Error? rolesError = ValidateRequestedRoles(user, command, requestedRoles);
        if (rolesError is not null) {
            return Result.Failure<RoleUpdate?>(rolesError);
        }

        IReadOnlyList<Role> roleEntities = await userManagementService.GetRolesByNamesAsync(requestedRoles, cancellationToken).ConfigureAwait(false);
        if (roleEntities.Count != requestedRoles.Length) {
            return Result.Failure<RoleUpdate?>(
                Errors.Validation.Invalid("roles", "One or more roles are not configured in the system."));
        }

        IReadOnlyList<UserRoleAuditEvent> roleAuditEvents = CreateRoleAuditEvents(
            user,
            roleEntities,
            command.ActorUserId.HasValue ? new UserId(command.ActorUserId.Value) : null,
            dateTimeProvider.GetUtcNow().UtcDateTime);

        return Result.Success<RoleUpdate?>(new RoleUpdate(roleEntities, roleAuditEvents));
    }

    private static Error? ValidateRequestedRoles(
        User user,
        UpdateAdminUserCommand command,
        IReadOnlyCollection<string> requestedRoles) {
        if (requestedRoles.Any(role => !AllowedRoles.Contains(role))) {
            return Errors.Validation.Invalid("roles", "Unknown role.");
        }

        bool isOwner = user.HasRole(RoleNames.Owner);
        bool requestsOwner = requestedRoles.Contains(RoleNames.Owner, StringComparer.Ordinal);
        bool requestsAdmin = requestedRoles.Contains(RoleNames.Admin, StringComparer.Ordinal);

        if (IsSelfUpdate(command) && user.HasRole(RoleNames.Admin) && !requestsAdmin) {
            return Errors.Validation.Invalid("roles", "Admin users cannot remove their own Admin role.");
        }

        if (!isOwner && requestsOwner) {
            return Errors.Validation.Invalid("roles", "Owner role cannot be assigned from the admin user editor.");
        }

        return isOwner && (!requestsOwner || !requestsAdmin)
            ? Errors.Validation.Invalid("roles", "Owner users must keep Owner and Admin roles.")
            : null;
    }

    private static Error? ApplyLifecycleUpdate(User user, UpdateAdminUserCommand command) {
        if (!command.IsActive.HasValue) {
            return null;
        }

        if (user.DeletedAt is not null) {
            return Errors.Validation.Invalid(
                nameof(command.IsActive),
                "Deleted user lifecycle cannot be changed via admin active toggle. Use restore flow first.");
        }

        if (command.IsActive.Value) {
            user.Activate();
            return null;
        }

        Error? deactivateError = ValidateDeactivation(user, command);
        if (deactivateError is not null) {
            return deactivateError;
        }

        user.Deactivate();
        return null;
    }

    private static Error? ValidateDeactivation(User user, UpdateAdminUserCommand command) {
        if (IsSelfUpdate(command)) {
            return Errors.Validation.Invalid(nameof(command.IsActive), "Admin users cannot deactivate their own account.");
        }

        return user.HasRole(RoleNames.Owner)
            ? Errors.Validation.Invalid(nameof(command.IsActive), "Owner user cannot be deactivated.")
            : null;
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

        IEnumerable<UserRoleAuditEvent> addedEvents = requestedRolesByName
            .Where(item => !currentRolesByName.ContainsKey(item.Key))
            .Select(item => UserRoleAuditEvent.Create(
                user.Id,
                item.Value,
                UserRoleAuditAction.Added,
                actorUserId,
                RoleAuditSource,
                occurredAtUtc));

        IEnumerable<UserRoleAuditEvent> removedEvents = currentRolesByName
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
