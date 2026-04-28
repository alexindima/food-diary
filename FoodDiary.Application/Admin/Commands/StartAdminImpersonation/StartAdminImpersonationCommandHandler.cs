using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Common.Abstractions.Audit;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Domain.Entities.Admin;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Admin.Commands.StartAdminImpersonation;

public sealed class StartAdminImpersonationCommandHandler(
    IUserRepository userRepository,
    IAdminImpersonationSessionRepository sessionRepository,
    IJwtTokenGenerator jwtTokenGenerator,
    IDateTimeProvider dateTimeProvider,
    IAuditLogger auditLogger)
    : ICommandHandler<StartAdminImpersonationCommand, Result<AdminImpersonationStartModel>> {
    public async Task<Result<AdminImpersonationStartModel>> Handle(
        StartAdminImpersonationCommand command,
        CancellationToken cancellationToken) {
        if (command.ActorUserId == Guid.Empty) {
            return Result.Failure<AdminImpersonationStartModel>(
                Errors.Validation.Invalid(nameof(command.ActorUserId), "Actor user id must not be empty."));
        }

        if (command.TargetUserId == Guid.Empty) {
            return Result.Failure<AdminImpersonationStartModel>(
                Errors.Validation.Invalid(nameof(command.TargetUserId), "Target user id must not be empty."));
        }

        if (command.ActorUserId == command.TargetUserId) {
            return Result.Failure<AdminImpersonationStartModel>(
                Errors.Validation.Invalid(nameof(command.TargetUserId), "Actor and target users must be different."));
        }

        var reason = command.Reason.Trim();
        var actorUserId = new UserId(command.ActorUserId);
        var targetUserId = new UserId(command.TargetUserId);

        var actor = await userRepository.GetByIdAsync(actorUserId, cancellationToken);
        if (actor is null || !actor.HasRole(RoleNames.Admin)) {
            return Result.Failure<AdminImpersonationStartModel>(Errors.Authentication.ImpersonationForbidden);
        }

        var target = await userRepository.GetByIdAsync(targetUserId, cancellationToken);
        if (target is null) {
            return Result.Failure<AdminImpersonationStartModel>(Errors.User.NotFound(command.TargetUserId));
        }

        if (target.HasRole(RoleNames.Admin)) {
            return Result.Failure<AdminImpersonationStartModel>(Errors.Authentication.ImpersonationForbidden);
        }

        var roles = target.GetRoleNames().ToArray();
        var token = jwtTokenGenerator.GenerateAccessToken(
            target.Id,
            target.Email,
            roles,
            new JwtImpersonationContext(actorUserId, reason));

        var session = AdminImpersonationSession.Start(
            actorUserId,
            target.Id,
            reason,
            command.ActorIpAddress,
            command.ActorUserAgent,
            dateTimeProvider.UtcNow);
        await sessionRepository.AddAsync(session, cancellationToken);

        auditLogger.Log(
            "admin.user.impersonation.start",
            actorUserId,
            "User",
            target.Id.Value.ToString(),
            $"targetEmail={target.Email} reason={reason}");

        return Result.Success(new AdminImpersonationStartModel(
            token,
            target.Id.Value,
            target.Email,
            actorUserId.Value,
            reason));
    }
}
