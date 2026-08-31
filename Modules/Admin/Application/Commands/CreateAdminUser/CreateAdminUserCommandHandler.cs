using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Audit;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Admin.Mappings;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Commands.CreateAdminUser;

public sealed class CreateAdminUserCommandHandler(
    IUserAdministrationMutationService userManagementService,
    IEmailSender emailSender,
    IAuditLogger auditLogger,
    TimeProvider timeProvider)
    : ICommandHandler<CreateAdminUserCommand, Result<AdminUserCreationModel>> {
    public async Task<Result<AdminUserCreationModel>> Handle(
        CreateAdminUserCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> actorIdResult = UserIdParser.Parse(
            command.ActorUserId,
            Errors.Validation.Invalid(nameof(command.ActorUserId), "Actor user id must not be empty."));
        if (actorIdResult.IsFailure) {
            return Result.Failure<AdminUserCreationModel>(actorIdResult.Error);
        }

        string temporaryPassword = ResolveTemporaryPassword(command);
        Result<UserAdminReadModel> creationResult = await userManagementService
            .CreateAsync(
                new UserAdminCreateModel(
                    command.Email,
                    command.FirstName,
                    command.LastName,
                    command.Language,
                    command.Roles,
                    temporaryPassword,
                    command.IsEmailConfirmed,
                    command.RequirePasswordChange,
                    actorIdResult.Value,
                    timeProvider.GetUtcNow().UtcDateTime),
                cancellationToken)
            .ConfigureAwait(false);
        if (creationResult.IsFailure) {
            return Result.Failure<AdminUserCreationModel>(creationResult.Error);
        }

        UserAdminReadModel user = creationResult.Value;
        await SendCredentialsAsync(command, user, temporaryPassword, cancellationToken).ConfigureAwait(false);

        auditLogger.Log(
            "admin.user.created",
            actorIdResult.Value,
            "User",
            user.Id.ToString(),
            $"roles={string.Join(',', user.Roles)} emailConfirmed={command.IsEmailConfirmed} credentialsEmailQueued={command.SendCredentialsEmail}");

        return Result.Success(new AdminUserCreationModel(
            user.ToAdminModel(),
            temporaryPassword,
            command.SendCredentialsEmail));
    }

    private static string ResolveTemporaryPassword(CreateAdminUserCommand command) =>
        command.GeneratePassword
            ? SecurityTokenGenerator.GenerateUrlSafeToken(18)
            : command.TemporaryPassword!.Trim();

    private Task SendCredentialsAsync(
        CreateAdminUserCommand command,
        UserAdminReadModel user,
        string temporaryPassword,
        CancellationToken cancellationToken) =>
        command.SendCredentialsEmail
            ? emailSender.SendAccountCreatedAsync(
                new AccountCreatedMessage(user.Email, temporaryPassword, user.Language, command.ClientOrigin),
                cancellationToken)
            : Task.CompletedTask;
}
