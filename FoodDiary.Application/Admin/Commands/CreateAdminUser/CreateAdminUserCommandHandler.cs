using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Audit;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Admin.Mappings;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Commands.CreateAdminUser;

public sealed class CreateAdminUserCommandHandler(
    IAdminUserManagementService userManagementService,
    IPasswordHasher passwordHasher,
    IEmailSender emailSender,
    IAuditLogger auditLogger,
    TimeProvider timeProvider)
    : ICommandHandler<CreateAdminUserCommand, Result<AdminUserCreationModel>> {
    private const string RoleAuditSource = "AdminUserCreator";

    public async Task<Result<AdminUserCreationModel>> Handle(
        CreateAdminUserCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> actorIdResult = UserIdParser.Parse(
            command.ActorUserId,
            Errors.Validation.Invalid(nameof(command.ActorUserId), "Actor user id must not be empty."));
        if (actorIdResult.IsFailure) {
            return Result.Failure<AdminUserCreationModel>(actorIdResult.Error);
        }

        User? existingUser = await userManagementService
            .GetByEmailIncludingDeletedAsync(command.Email, cancellationToken)
            .ConfigureAwait(false);
        if (existingUser is not null) {
            return Result.Failure<AdminUserCreationModel>(Errors.User.EmailAlreadyExists);
        }

        string[] requestedRoleNames = [.. command.Roles
            .Where(static role => !string.IsNullOrWhiteSpace(role))
            .Select(static role => role.Trim())
            .Distinct(StringComparer.Ordinal)];
        IReadOnlyList<Role> roles = await userManagementService
            .GetRolesByNamesAsync(requestedRoleNames, cancellationToken)
            .ConfigureAwait(false);
        if (roles.Count != requestedRoleNames.Length) {
            return Result.Failure<AdminUserCreationModel>(
                Errors.Validation.Invalid(nameof(command.Roles), "One or more roles are not configured in the system."));
        }

        string temporaryPassword = ResolveTemporaryPassword(command);
        User user = CreateUser(command, roles, temporaryPassword);

        user = await userManagementService.AddAsync(user, cancellationToken).ConfigureAwait(false);
        UserRoleAuditEvent[] roleAuditEvents = CreateRoleAuditEvents(user, roles, actorIdResult.Value);
        await userManagementService.UpdateAsync(user, roleAuditEvents, cancellationToken).ConfigureAwait(false);

        await SendCredentialsAsync(command, user, temporaryPassword, cancellationToken).ConfigureAwait(false);

        auditLogger.Log(
            "admin.user.created",
            actorIdResult.Value,
            "User",
            user.Id.Value.ToString(),
            $"roles={string.Join(',', requestedRoleNames)} emailConfirmed={command.IsEmailConfirmed} credentialsEmailQueued={command.SendCredentialsEmail}");

        return Result.Success(new AdminUserCreationModel(
            user.ToAdminModel(),
            temporaryPassword,
            command.SendCredentialsEmail));
    }

    private User CreateUser(
        CreateAdminUserCommand command,
        IReadOnlyCollection<Role> roles,
        string temporaryPassword) {
        var user = User.Create(command.Email, passwordHasher.Hash(temporaryPassword));
        user.UpdatePersonalInfo(firstName: command.FirstName, lastName: command.LastName);
        user.UpdateGoals(new UserGoalUpdate(
            DailyCalorieTarget: 2000,
            ProteinTarget: 150,
            FatTarget: 65,
            CarbTarget: 200,
            FiberTarget: 28,
            WaterGoal: 2000));
        user.SetLanguage(LanguageCode.FromPreferred(command.Language).Value);
        user.SetEmailConfirmed(command.IsEmailConfirmed);
        user.ReplaceRoles(roles);
        if (command.RequirePasswordChange) {
            user.RequirePasswordChange();
        }

        return user;
    }

    private static string ResolveTemporaryPassword(CreateAdminUserCommand command) =>
        command.GeneratePassword
            ? SecurityTokenGenerator.GenerateUrlSafeToken(18)
            : command.TemporaryPassword!.Trim();

    private UserRoleAuditEvent[] CreateRoleAuditEvents(
        User user,
        IEnumerable<Role> roles,
        UserId actorUserId) =>
        [.. roles.Select(role => UserRoleAuditEvent.Create(
            user.Id,
            role,
            UserRoleAuditAction.Added,
            actorUserId,
            RoleAuditSource,
            timeProvider.GetUtcNow().UtcDateTime))];

    private Task SendCredentialsAsync(
        CreateAdminUserCommand command,
        User user,
        string temporaryPassword,
        CancellationToken cancellationToken) =>
        command.SendCredentialsEmail
            ? emailSender.SendAccountCreatedAsync(
                new AccountCreatedMessage(user.Email, temporaryPassword, user.Language, command.ClientOrigin),
                cancellationToken)
            : Task.CompletedTask;
}
