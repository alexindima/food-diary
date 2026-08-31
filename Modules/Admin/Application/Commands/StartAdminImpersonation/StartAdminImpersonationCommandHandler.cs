using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Audit;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Domain.Entities.Admin;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Admin.Commands.StartAdminImpersonation;

public sealed class StartAdminImpersonationCommandHandler(
    IUserAuthenticationIdentityService userIdentityService,
    IAdminImpersonationSessionWriteRepository sessionRepository,
    IAdminImpersonationHandoffService handoffService,
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
        Result<UserAuthenticationPrincipalModel> actorResult = await LoadActorAsync(actorUserId, cancellationToken).ConfigureAwait(false);
        if (actorResult.IsFailure) {
            return Result.Failure<AdminImpersonationStartModel>(actorResult.Error);
        }

        Result<UserAuthenticationPrincipalModel> targetResult = await LoadTargetAsync(targetUserId, command.TargetUserId, cancellationToken).ConfigureAwait(false);
        if (targetResult.IsFailure) {
            return Result.Failure<AdminImpersonationStartModel>(targetResult.Error);
        }

        UserAuthenticationPrincipalModel target = targetResult.Value;
        string token = GenerateToken(target, actorUserId, reason);
        string code = await handoffService.CreateCodeAsync(token, cancellationToken).ConfigureAwait(false);
        await StartSessionAsync(command, actorUserId, target.UserId, reason, cancellationToken).ConfigureAwait(false);
        LogStart(actorUserId, target, reason);

        return Result.Success(new AdminImpersonationStartModel(
            code,
            target.UserId.Value,
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

    private async Task<Result<UserAuthenticationPrincipalModel>> LoadActorAsync(UserId actorUserId, CancellationToken cancellationToken) {
        Result<UserAuthenticationPrincipalModel> result = await userIdentityService
            .GetAuthenticationPrincipalAsync(actorUserId, dateTimeProvider.GetUtcNow().UtcDateTime, cancellationToken)
            .ConfigureAwait(false);
        if (result.IsFailure || !result.Value.Roles.Contains(RoleNames.Admin, StringComparer.Ordinal)) {
            return Result.Failure<UserAuthenticationPrincipalModel>(Errors.Authentication.ImpersonationForbidden);
        }

        return result;
    }

    private async Task<Result<UserAuthenticationPrincipalModel>> LoadTargetAsync(
        UserId targetUserId,
        Guid targetId,
        CancellationToken cancellationToken) {
        Result<UserAuthenticationPrincipalModel> result = await userIdentityService
            .GetAuthenticationPrincipalAsync(targetUserId, dateTimeProvider.GetUtcNow().UtcDateTime, cancellationToken)
            .ConfigureAwait(false);
        if (result.IsFailure) {
            return string.Equals(result.Error.Code, "User.NotFound", StringComparison.Ordinal)
                ? Result.Failure<UserAuthenticationPrincipalModel>(Errors.User.NotFound(targetId))
                : Result.Failure<UserAuthenticationPrincipalModel>(Errors.Authentication.ImpersonationForbidden);
        }

        if (result.Value.Roles.Contains(RoleNames.Admin, StringComparer.Ordinal)) {
            return Result.Failure<UserAuthenticationPrincipalModel>(Errors.Authentication.ImpersonationForbidden);
        }

        return result;
    }

    private string GenerateToken(UserAuthenticationPrincipalModel target, UserId actorUserId, string reason) {
        return jwtTokenGenerator.GenerateAccessToken(
            target.UserId,
            target.Email,
            target.Roles,
            new JwtImpersonationContext(actorUserId, reason),
            target.SecurityVersion);
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

    private void LogStart(UserId actorUserId, UserAuthenticationPrincipalModel target, string reason) {
        auditLogger.Log(
            "admin.user.impersonation.start",
            actorUserId,
            "User",
            target.UserId.Value.ToString(),
            $"targetEmail={target.Email} reason={reason}");
    }
}
