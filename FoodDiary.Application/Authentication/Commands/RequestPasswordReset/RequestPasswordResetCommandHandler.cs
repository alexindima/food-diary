using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Authentication.Commands.RequestPasswordReset;

public sealed class RequestPasswordResetCommandHandler(
    IAuthenticationUserMutationService userMutationService,
    IPasswordHasher passwordHasher,
    IEmailSender emailSender,
    TimeProvider dateTimeProvider,
    ILogger<RequestPasswordResetCommandHandler> logger)
    : ICommandHandler<RequestPasswordResetCommand, Result> {
    private static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(1);

    public async Task<Result> Handle(RequestPasswordResetCommand command, CancellationToken cancellationToken) {
        User? user = await userMutationService.GetByEmailIncludingDeletedAsync(command.Email, cancellationToken).ConfigureAwait(false);
        if (!AuthenticationUserAccessPolicy.CanRequestPasswordReset(user)) {
            return Result.Success();
        }

        User currentUser = user!;
        DateTime nowUtc = dateTimeProvider.GetUtcNow().UtcDateTime;
        if (currentUser.PasswordResetSentAtUtc.HasValue &&
            nowUtc - currentUser.PasswordResetSentAtUtc.Value < Cooldown) {
            logger.LogInformation("Password reset request throttled by cooldown.");
            return Result.Success();
        }

        string token = SecurityTokenGenerator.GenerateUrlSafeToken();
        string tokenHash = passwordHasher.Hash(token);
        DateTime expiresAtUtc = nowUtc.Add(TokenLifetime);

        currentUser.SetPasswordResetToken(new UserTokenIssue(
            TokenHash: tokenHash,
            ExpiresAtUtc: expiresAtUtc,
            IssuedAtUtc: nowUtc));
        await userMutationService.UpdateAsync(currentUser, cancellationToken).ConfigureAwait(false);

        PasswordResetMessage message = new(currentUser.Email, currentUser.Id.Value.ToString(), token, currentUser.Language, command.ClientOrigin);
        await emailSender.SendPasswordResetAsync(message, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
