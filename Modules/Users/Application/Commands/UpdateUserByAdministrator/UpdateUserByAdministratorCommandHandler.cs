using FoodDiary.Modules.Users.Application.Mappings;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Modules.Users.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Application.Common;

using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Enums;
using FoodDiary.Modules.Users.Domain.ValueObjects;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Commands.UpdateUserByAdministrator;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Users.Application.Commands.UpdateUserByAdministrator;

public sealed class UpdateUserByAdministratorCommandHandler(IUserLookupRepository userLookupRepository,
    IUserWriteRepository userWriteRepository,
    IUserRoleCatalogService roleCatalogService) : IRequestHandler<UpdateUserByAdministratorCommand, Result<UserAdminReadModel>> {
    public async Task<Result<UserAdminReadModel>> Handle(UpdateUserByAdministratorCommand request, CancellationToken cancellationToken) {
        UserAdminUpdateModel input = request.Request;
        User? user = await userLookupRepository
            .GetByIdIncludingDeletedAsync(input.UserId, cancellationToken)
            .ConfigureAwait(false);
        if (user is null) {
            return Result.Failure<UserAdminReadModel>(UserErrors.NotFound(input.UserId));
        }

        Result<string?> languageResult = UserPreferenceCodeParser.ParseOptionalLanguage(
            input.Language,
            "language",
            "Invalid language value.");
        if (languageResult.IsFailure) {
            return Result.Failure<UserAdminReadModel>(languageResult.Error);
        }

        Result<RoleUpdate?> roleUpdateResult = await PrepareRoleUpdateAsync(user, input, cancellationToken).ConfigureAwait(false);
        if (roleUpdateResult.IsFailure) {
            return Result.Failure<UserAdminReadModel>(roleUpdateResult.Error);
        }

        Error? lifecycleError = ApplyLifecycleUpdate(user, input);
        if (lifecycleError is not null) {
            return Result.Failure<UserAdminReadModel>(lifecycleError);
        }

        user.UpdateAdminSecurity(new UserAdminSecurityUpdate(input.IsEmailConfirmed));
        user.UpdateAdminPreferences(new UserAdminPreferenceUpdate(languageResult.Value));
        user.UpdateAdminAiQuota(new UserAdminAiQuotaUpdate(input.AiInputTokenLimit, input.AiOutputTokenLimit));
        if (roleUpdateResult.Value is not null) {
            user.ReplaceRoles(roleUpdateResult.Value.Roles);
        }

        await userWriteRepository.UpdateAsync(
            user,
            roleUpdateResult.Value?.AuditEvents ?? [],
            cancellationToken).ConfigureAwait(false);
        return Result.Success(user.ToAdminReadModel());

    }

    private async Task<Result<RoleUpdate?>> PrepareRoleUpdateAsync(
        User user,
        UserAdminUpdateModel request,
        CancellationToken cancellationToken) {
        if (request.Roles is null) {
            return Result.Success<RoleUpdate?>(value: null);
        }

        string[] requestedRoles = NormalizeRoles(request.Roles);
        Error? error = ValidateRequestedRoles(user, request, requestedRoles);
        if (error is not null) {
            return Result.Failure<RoleUpdate?>(error);
        }

        IReadOnlyList<Role> roles = await roleCatalogService
            .GetRolesByNamesAsync(requestedRoles, cancellationToken)
            .ConfigureAwait(false);
        if (roles.Count != requestedRoles.Length) {
            return Result.Failure<RoleUpdate?>(Errors.Validation.Invalid("roles", "One or more roles are not configured in the system."));
        }

        return Result.Success<RoleUpdate?>(new RoleUpdate(
            roles,
            CreateRoleAuditEvents(user, roles, request.ActorUserId, request.UpdatedAtUtc)));
    }

    private static Error? ApplyLifecycleUpdate(User user, UserAdminUpdateModel request) {
        if (!request.IsActive.HasValue) {
            return null;
        }

        if (user.DeletedAt is not null) {
            return Errors.Validation.Invalid("IsActive", "Deleted user lifecycle cannot be changed via admin active toggle. Use restore flow first.");
        }

        if (request.IsActive.Value) {
            user.Activate();
            return null;
        }

        if (request.ActorUserId == request.UserId) {
            return Errors.Validation.Invalid("IsActive", "Admin users cannot deactivate their own account.");
        }

        if (user.HasRole(RoleNames.Owner)) {
            return Errors.Validation.Invalid("IsActive", "Owner user cannot be deactivated.");
        }

        user.Deactivate();
        return null;
    }

    private sealed record RoleUpdate(IReadOnlyList<Role> Roles, IReadOnlyList<UserRoleAuditEvent> AuditEvents);

    private static Error? ValidateRequestedRoles(User user, UserAdminUpdateModel request, IReadOnlyCollection<string> roles) {
        if (roles.Any(role => !AllowedRoles.Contains(role))) {
            return Errors.Validation.Invalid("roles", "Unknown role.");
        }

        bool isSelfUpdate = request.ActorUserId == request.UserId;
        bool requestsOwner = roles.Contains(RoleNames.Owner, StringComparer.Ordinal);
        bool requestsAdmin = roles.Contains(RoleNames.Admin, StringComparer.Ordinal);
        if (isSelfUpdate && user.HasRole(RoleNames.Admin) && !requestsAdmin) {
            return Errors.Validation.Invalid("roles", "Admin users cannot remove their own Admin role.");
        }

        if (!user.HasRole(RoleNames.Owner) && requestsOwner) {
            return Errors.Validation.Invalid("roles", "Owner role cannot be assigned from the admin user editor.");
        }

        return user.HasRole(RoleNames.Owner) && (!requestsOwner || !requestsAdmin)
            ? Errors.Validation.Invalid("roles", "Owner users must keep Owner and Admin roles.")
            : null;
    }

    private static IReadOnlyList<UserRoleAuditEvent> CreateRoleAuditEvents(
        User user,
        IReadOnlyCollection<Role> requestedRoles,
        FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids.UserId? actorUserId,
        DateTime occurredAtUtc) {
        var current = user.UserRoles.Select(userRole => userRole.Role).ToDictionary(role => role.Name, StringComparer.Ordinal);
        var requested = requestedRoles.ToDictionary(role => role.Name, StringComparer.Ordinal);
        return [.. requested.Where(item => !current.ContainsKey(item.Key))
            .Select(item => UserRoleAuditEvent.Create(user.Id, item.Value, UserRoleAuditAction.Added, actorUserId, EditorAuditSource, occurredAtUtc))
            .Concat(current.Where(item => !requested.ContainsKey(item.Key))
                .Select(item => UserRoleAuditEvent.Create(user.Id, item.Value, UserRoleAuditAction.Removed, actorUserId, EditorAuditSource, occurredAtUtc)))
            .OrderBy(auditEvent => auditEvent.RoleName, StringComparer.Ordinal)
            .ThenBy(auditEvent => auditEvent.Action)];
    }

    private static string[] NormalizeRoles(IEnumerable<string> roles) =>
        [.. roles.Where(role => !string.IsNullOrWhiteSpace(role)).Select(role => role.Trim()).Distinct(StringComparer.Ordinal)];

    private const string EditorAuditSource = "AdminUserEditor";

    private static readonly HashSet<string> AllowedRoles = new(
        [RoleNames.Owner, RoleNames.Admin, RoleNames.Premium, RoleNames.Support, RoleNames.Dietologist],
        StringComparer.Ordinal);

}
