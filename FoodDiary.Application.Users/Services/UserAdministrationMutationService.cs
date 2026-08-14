using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Results;

namespace FoodDiary.Application.Users.Services;

internal sealed class UserAdministrationMutationService(
    IUserLookupRepository userLookupRepository,
    IUserWriteRepository userWriteRepository,
    IUserRoleCatalogService roleCatalogService,
    IPasswordHasher passwordHasher) : IUserAdministrationMutationService {
    private const string CreatorAuditSource = "AdminUserCreator";
    private const string EditorAuditSource = "AdminUserEditor";

    private static readonly HashSet<string> AllowedRoles = new(
        [RoleNames.Owner, RoleNames.Admin, RoleNames.Premium, RoleNames.Support, RoleNames.Dietologist],
        StringComparer.Ordinal);

    public async Task<Result<UserAdminReadModel>> CreateAsync(
        UserAdminCreateModel request,
        CancellationToken cancellationToken = default) {
        User? existingUser = await userLookupRepository
            .GetByEmailIncludingDeletedAsync(request.Email, cancellationToken)
            .ConfigureAwait(false);
        if (existingUser is not null) {
            return Result.Failure<UserAdminReadModel>(Errors.User.EmailAlreadyExists);
        }

        string[] requestedRoles = NormalizeRoles(request.Roles);
        IReadOnlyList<Role> roles = await roleCatalogService
            .GetRolesByNamesAsync(requestedRoles, cancellationToken)
            .ConfigureAwait(false);
        if (roles.Count != requestedRoles.Length) {
            return Result.Failure<UserAdminReadModel>(
                Errors.Validation.Invalid("Roles", "One or more roles are not configured in the system."));
        }

        var user = User.Create(request.Email, passwordHasher.Hash(request.TemporaryPassword));
        user.UpdatePersonalInfo(firstName: request.FirstName, lastName: request.LastName);
        user.UpdateGoals(new UserGoalUpdate(
            DailyCalorieTarget: 2000,
            ProteinTarget: 150,
            FatTarget: 65,
            CarbTarget: 200,
            FiberTarget: 28,
            WaterGoal: 2000));
        user.SetLanguage(LanguageCode.FromPreferred(request.Language).Value);
        user.SetEmailConfirmed(request.IsEmailConfirmed);
        user.ReplaceRoles(roles);
        if (request.RequirePasswordChange) {
            user.RequirePasswordChange();
        }

        await userWriteRepository.AddAsync(user, cancellationToken).ConfigureAwait(false);
        UserRoleAuditEvent[] auditEvents = [.. roles.Select(role => UserRoleAuditEvent.Create(
            user.Id,
            role,
            UserRoleAuditAction.Added,
            request.ActorUserId,
            CreatorAuditSource,
            request.CreatedAtUtc))];
        await userWriteRepository.UpdateAsync(user, auditEvents, cancellationToken).ConfigureAwait(false);
        return Result.Success(user.ToAdminReadModel());
    }

    public async Task<Result<UserAdminReadModel>> UpdateAsync(
        UserAdminUpdateModel request,
        CancellationToken cancellationToken = default) {
        User? user = await userLookupRepository
            .GetByIdIncludingDeletedAsync(request.UserId, cancellationToken)
            .ConfigureAwait(false);
        if (user is null) {
            return Result.Failure<UserAdminReadModel>(Errors.User.NotFound(request.UserId));
        }

        Result<string?> languageResult = UserPreferenceCodeParser.ParseOptionalLanguage(
            request.Language,
            "language",
            "Invalid language value.");
        if (languageResult.IsFailure) {
            return Result.Failure<UserAdminReadModel>(languageResult.Error);
        }

        Result<RoleUpdate?> roleUpdateResult = await PrepareRoleUpdateAsync(user, request, cancellationToken).ConfigureAwait(false);
        if (roleUpdateResult.IsFailure) {
            return Result.Failure<UserAdminReadModel>(roleUpdateResult.Error);
        }

        Error? lifecycleError = ApplyLifecycleUpdate(user, request);
        if (lifecycleError is not null) {
            return Result.Failure<UserAdminReadModel>(lifecycleError);
        }

        user.UpdateAdminSecurity(new UserAdminSecurityUpdate(request.IsEmailConfirmed));
        user.UpdateAdminPreferences(new UserAdminPreferenceUpdate(languageResult.Value));
        user.UpdateAdminAiQuota(new UserAdminAiQuotaUpdate(request.AiInputTokenLimit, request.AiOutputTokenLimit));
        if (roleUpdateResult.Value is not null) {
            user.ReplaceRoles(roleUpdateResult.Value.Roles);
        }

        await userWriteRepository.UpdateAsync(
            user,
            roleUpdateResult.Value?.AuditEvents ?? [],
            cancellationToken).ConfigureAwait(false);
        return Result.Success(user.ToAdminReadModel());
    }

    public async Task<Result> SetPasswordAsync(
        FoodDiary.Domain.ValueObjects.Ids.UserId userId,
        string newPassword,
        CancellationToken cancellationToken = default) {
        User? user = await userLookupRepository
            .GetByIdIncludingDeletedAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        if (user is null) {
            return Result.Failure(Errors.User.NotFound(userId));
        }

        user.UpdatePassword(passwordHasher.Hash(newPassword));
        await userWriteRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        return Result.Success();
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

    private static IReadOnlyList<UserRoleAuditEvent> CreateRoleAuditEvents(
        User user,
        IReadOnlyCollection<Role> requestedRoles,
        FoodDiary.Domain.ValueObjects.Ids.UserId? actorUserId,
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

    private sealed record RoleUpdate(IReadOnlyList<Role> Roles, IReadOnlyList<UserRoleAuditEvent> AuditEvents);
}
