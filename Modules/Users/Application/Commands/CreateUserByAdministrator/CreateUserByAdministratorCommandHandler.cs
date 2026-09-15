using FoodDiary.Modules.Users.Application.Mappings;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Modules.Users.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;

using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Users.Domain.ValueObjects;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Commands.CreateUserByAdministrator;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Users.Application.Commands.CreateUserByAdministrator;

public sealed class CreateUserByAdministratorCommandHandler(IUserLookupRepository userLookupRepository,
    IUserWriteRepository userWriteRepository,
    IUserRoleCatalogService roleCatalogService,
    IPasswordHasher passwordHasher) : IRequestHandler<CreateUserByAdministratorCommand, Result<UserAdminReadModel>> {
    public async Task<Result<UserAdminReadModel>> Handle(CreateUserByAdministratorCommand request, CancellationToken cancellationToken) {
        UserAdminCreateModel input = request.Request;
        User? existingUser = await userLookupRepository
            .GetByEmailIncludingDeletedAsync(input.Email, cancellationToken)
            .ConfigureAwait(false);
        if (existingUser is not null) {
            return Result.Failure<UserAdminReadModel>(UserErrors.EmailAlreadyExists);
        }

        string[] requestedRoles = NormalizeRoles(input.Roles);
        if (requestedRoles.Any(role => !CreatableRoles.Contains(role))) {
            return Result.Failure<UserAdminReadModel>(Errors.Validation.Invalid("Roles", "Unknown or protected role."));
        }

        IReadOnlyList<Role> roles = await roleCatalogService
            .GetRolesByNamesAsync(requestedRoles, cancellationToken)
            .ConfigureAwait(false);
        if (roles.Count != requestedRoles.Length) {
            return Result.Failure<UserAdminReadModel>(
                Errors.Validation.Invalid("Roles", "One or more roles are not configured in the system."));
        }

        var user = User.Create(input.Email, passwordHasher.Hash(input.TemporaryPassword));
        user.UpdatePersonalInfo(firstName: input.FirstName, lastName: input.LastName);
        user.UpdateGoals(new UserGoalUpdate(
            DailyCalorieTarget: 2000,
            ProteinTarget: 150,
            FatTarget: 65,
            CarbTarget: 200,
            FiberTarget: 28,
            WaterGoal: 2000));
        user.SetLanguage(LanguageCode.FromPreferred(input.Language).Value);
        user.SetEmailConfirmed(input.IsEmailConfirmed);
        user.ReplaceRoles(roles);
        if (input.RequirePasswordChange) {
            user.RequirePasswordChange();
        }

        await userWriteRepository.AddAsync(user, cancellationToken).ConfigureAwait(false);
        UserRoleAuditEvent[] auditEvents = [.. roles.Select(role => UserRoleAuditEvent.Create(
            user.Id,
            role,
            UserRoleAuditAction.Added,
            input.ActorUserId,
            CreatorAuditSource,
            input.CreatedAtUtc))];
        await userWriteRepository.UpdateAsync(user, auditEvents, cancellationToken).ConfigureAwait(false);
        return Result.Success(user.ToAdminReadModel());

    }
    private const string CreatorAuditSource = "AdminUserCreator";

    private static readonly HashSet<string> CreatableRoles = new(
        [RoleNames.Admin, RoleNames.Premium, RoleNames.Support, RoleNames.Dietologist],
        StringComparer.Ordinal);

    private static string[] NormalizeRoles(IEnumerable<string> roles) =>
        [.. roles.Where(role => !string.IsNullOrWhiteSpace(role)).Select(role => role.Trim()).Distinct(StringComparer.Ordinal)];

}
