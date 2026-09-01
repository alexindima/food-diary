using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Identity.Authentication.Commands.Logout;

public sealed class LogoutCommandHandler(
    IJwtTokenGenerator jwtTokenGenerator,
    IRefreshTokenSessionWriteRepository repository,
    TimeProvider timeProvider) : ICommandHandler<LogoutCommand, Result> {
    public async Task<Result> Handle(LogoutCommand command, CancellationToken cancellationToken) {
        if (string.IsNullOrWhiteSpace(command.RefreshToken)) {
            return Result.Success();
        }

        (UserId userId, string email, bool rememberMe, Guid? refreshSessionId)? validationResult =
            jwtTokenGenerator.ValidateToken(command.RefreshToken);
        if (validationResult?.refreshSessionId is not Guid sessionId) {
            return Result.Success();
        }

        UserId userId = validationResult.GetValueOrDefault().userId;
        await repository.RevokeByIdAsync(
            sessionId,
            userId,
            timeProvider.GetUtcNow().UtcDateTime,
            cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
