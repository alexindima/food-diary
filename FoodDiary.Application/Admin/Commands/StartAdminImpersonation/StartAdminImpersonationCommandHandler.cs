using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Common.Abstractions.Audit;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Domain.Entities.Admin;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Admin.Commands.StartAdminImpersonation;

public sealed class StartAdminImpersonationCommandHandler(
    IUserRepository userRepository,
    IAdminImpersonationSessionRepository sessionRepository,
    IJwtTokenGenerator jwtTokenGenerator,
    TimeProvider dateTimeProvider,
    IAuditLogger auditLogger)
    : ICommandHandler<StartAdminImpersonationCommand, Result<AdminImpersonationStartModel>> {
    public async Task<Result<AdminImpersonationStartModel>> Handle(
        StartAdminImpersonationCommand command,
        CancellationToken cancellationToken) {
        Error? validationError = ValidateCommand(command);
        if (validationError is not null) {
            return Result.Failure<AdminImpersonationStartModel>(validationError);
        }

        string reason = command.Reason.Trim();
        var actorUserId = new UserId(command.ActorUserId);
        var targetUserId = new UserId(command.TargetUserId);
        Result<User> actorResult = await LoadActorAsync(actorUserId, cancellationToken).ConfigureAwait(false);
        if (actorResult.IsFailure) {
            return Result.Failure<AdminImpersonationStartModel>(actorResult.Error);
        }

        Result<User> targetResult = await LoadTargetAsync(targetUserId, command.TargetUserId, cancellationToken).ConfigureAwait(false);
        if (targetResult.IsFailure) {
            return Result.Failure<AdminImpersonationStartModel>(targetResult.Error);
        }

        User target = targetResult.Value;
        string token = GenerateToken(target, actorUserId, reason);
        await StartSessionAsync(command, actorUserId, target.Id, reason, cancellationToken).ConfigureAwait(false);
        LogStart(actorUserId, target, reason);

        return Result.Success(new AdminImpersonationStartModel(
            token,
            target.Id.Value,
            target.Email,
            actorUserId.Value,
            reason));
    }

    private static Error? ValidateCommand(StartAdminImpersonationCommand command) {
        if (command.ActorUserId == Guid.Empty) {
            return Errors.Validation.Invalid(nameof(command.ActorUserId), "Actor user id must not be empty.");
        }

        if (command.TargetUserId == Guid.Empty) {
            return Errors.Validation.Invalid(nameof(command.TargetUserId), "Target user id must not be empty.");
        }

        if (command.ActorUserId == command.TargetUserId) {
            return Errors.Validation.Invalid(nameof(command.TargetUserId), "Actor and target users must be different.");
        }

        return null;
    }

    private async Task<Result<Domain.Entities.Users.User>> LoadActorAsync(UserId actorUserId, CancellationToken cancellationToken) {
        User? actor = await userRepository.GetByIdAsync(actorUserId, cancellationToken).ConfigureAwait(false);
        if (AuthenticationUserAccessPolicy.EnsureCanAuthenticate(actor) is not null
            || actor is null
            || !actor.HasRole(RoleNames.Admin)) {
            return Result.Failure<Domain.Entities.Users.User>(Errors.Authentication.ImpersonationForbidden);
        }

        return Result.Success(actor);
    }

    private async Task<Result<Domain.Entities.Users.User>> LoadTargetAsync(
        UserId targetUserId,
        Guid targetId,
        CancellationToken cancellationToken) {
        User? target = await userRepository.GetByIdAsync(targetUserId, cancellationToken).ConfigureAwait(false);
        if (target is null) {
            return Result.Failure<Domain.Entities.Users.User>(Errors.User.NotFound(targetId));
        }

        if (AuthenticationUserAccessPolicy.EnsureCanAuthenticate(target) is not null
            || target.HasRole(RoleNames.Admin)) {
            return Result.Failure<Domain.Entities.Users.User>(Errors.Authentication.ImpersonationForbidden);
        }

        return Result.Success(target);
    }

    private string GenerateToken(Domain.Entities.Users.User target, UserId actorUserId, string reason) {
        string[] roles = target.GetRoleNames().ToArray();
        return jwtTokenGenerator.GenerateAccessToken(
            target.Id,
            target.Email,
            roles,
            new JwtImpersonationContext(actorUserId, reason));
    }

    private async Task StartSessionAsync(
        StartAdminImpersonationCommand command,
        UserId actorUserId,
        UserId targetUserId,
        string reason,
        CancellationToken cancellationToken) {
        var session = AdminImpersonationSession.Start(
            actorUserId,
            targetUserId,
            reason,
            command.ActorIpAddress,
            command.ActorUserAgent,
            dateTimeProvider.GetUtcNow().UtcDateTime);
        await sessionRepository.AddAsync(session, cancellationToken).ConfigureAwait(false);
    }

    private void LogStart(UserId actorUserId, Domain.Entities.Users.User target, string reason) {
        auditLogger.Log(
            "admin.user.impersonation.start",
            actorUserId,
            "User",
            target.Id.Value.ToString(),
            $"targetEmail={target.Email} reason={reason}");
    }
}
