using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Common.Abstractions.Audit;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Domain.Entities.Admin;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Admin.Commands.StartAdminImpersonation;

public sealed class StartAdminImpersonationCommandHandler(
    IAdminImpersonationUserService userService,
    IAdminImpersonationSessionWriteRepository sessionRepository,
    IJwtTokenGenerator jwtTokenGenerator,
    TimeProvider dateTimeProvider,
    IAuditLogger auditLogger)
    : ICommandHandler<StartAdminImpersonationCommand, Result<AdminImpersonationStartModel>> {
    public async Task<Result<AdminImpersonationStartModel>> Handle(
        StartAdminImpersonationCommand command,
        CancellationToken cancellationToken) {
        Result<ImpersonationUserIds> userIdsResult = ValidateCommand(command);
        if (userIdsResult.IsFailure) {
            return Result.Failure<AdminImpersonationStartModel>(userIdsResult.Error);
        }

        string reason = command.Reason.Trim();
        UserId actorUserId = userIdsResult.Value.ActorUserId;
        UserId targetUserId = userIdsResult.Value.TargetUserId;
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

    private sealed record ImpersonationUserIds(UserId ActorUserId, UserId TargetUserId);

    private static Result<ImpersonationUserIds> ValidateCommand(StartAdminImpersonationCommand command) {
        Result<UserId> actorUserIdResult = UserIdParser.Parse(
            command.ActorUserId,
            Errors.Validation.Invalid(nameof(command.ActorUserId), "Actor user id must not be empty."));
        if (actorUserIdResult.IsFailure) {
            return UserIdParser.ToFailure<ImpersonationUserIds>(actorUserIdResult);
        }

        Result<UserId> targetUserIdResult = UserIdParser.Parse(
            command.TargetUserId,
            Errors.Validation.Invalid(nameof(command.TargetUserId), "Target user id must not be empty."));
        if (targetUserIdResult.IsFailure) {
            return UserIdParser.ToFailure<ImpersonationUserIds>(targetUserIdResult);
        }

        if (command.ActorUserId == command.TargetUserId) {
            return Result.Failure<ImpersonationUserIds>(
                Errors.Validation.Invalid(nameof(command.TargetUserId), "Actor and target users must be different."));
        }

        return Result.Success(new ImpersonationUserIds(actorUserIdResult.Value, targetUserIdResult.Value));
    }

    private async Task<Result<User>> LoadActorAsync(UserId actorUserId, CancellationToken cancellationToken) {
        User? actor = await userService.GetByIdAsync(actorUserId, cancellationToken).ConfigureAwait(false);
        if (AuthenticationUserAccessPolicy.EnsureCanAuthenticate(actor) is not null
            || actor?.HasRole(RoleNames.Admin) != true) {
            return Result.Failure<User>(Errors.Authentication.ImpersonationForbidden);
        }

        return Result.Success(actor);
    }

    private async Task<Result<User>> LoadTargetAsync(
        UserId targetUserId,
        Guid targetId,
        CancellationToken cancellationToken) {
        User? target = await userService.GetByIdAsync(targetUserId, cancellationToken).ConfigureAwait(false);
        if (target is null) {
            return Result.Failure<User>(Errors.User.NotFound(targetId));
        }

        if (AuthenticationUserAccessPolicy.EnsureCanAuthenticate(target) is not null
            || target.HasRole(RoleNames.Admin)) {
            return Result.Failure<User>(Errors.Authentication.ImpersonationForbidden);
        }

        return Result.Success(target);
    }

    private string GenerateToken(User target, UserId actorUserId, string reason) {
        string[] roles = [.. target.GetRoleNames()];
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

    private void LogStart(UserId actorUserId, User target, string reason) {
        auditLogger.Log(
            "admin.user.impersonation.start",
            actorUserId,
            "User",
            target.Id.Value.ToString(),
            $"targetEmail={target.Email} reason={reason}");
    }
}
