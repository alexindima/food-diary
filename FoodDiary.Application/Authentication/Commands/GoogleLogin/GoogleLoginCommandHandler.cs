using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Authentication.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Abstractions.Authentication.Services;
using FoodDiary.Domain.Entities.Notifications;

namespace FoodDiary.Application.Authentication.Commands.GoogleLogin;

public sealed class GoogleLoginCommandHandler(
    IUserAuthenticationIdentityService userIdentityService,
    INotificationDeduplicationService notificationDeduplicationService,
    INotificationWriter notificationWriter,
    IGoogleTokenValidator googleTokenValidator,
    TimeProvider dateTimeProvider,
    IAuthenticationTokenService authenticationTokenService)
    : ICommandHandler<GoogleLoginCommand, Result<AuthenticationModel>> {
    public async Task<Result<AuthenticationModel>> Handle(GoogleLoginCommand command, CancellationToken cancellationToken) {
        Result<GoogleIdentityPayload> payloadResult = await googleTokenValidator.ValidateCredentialAsync(command.Credential, cancellationToken).ConfigureAwait(false);
        if (!payloadResult.IsSuccess) {
            return Result.Failure<AuthenticationModel>(payloadResult.Error);
        }

        GoogleIdentityPayload payload = payloadResult.Value;
        Result<UserAuthenticationPrincipalModel> authenticationResult = await userIdentityService
            .AuthenticateGoogleAsync(
                new UserGoogleAuthenticationModel(
                    payload.Issuer,
                    payload.Subject,
                    payload.Email,
                    payload.FirstName,
                    payload.LastName,
                    payload.Locale),
                dateTimeProvider.GetUtcNow().UtcDateTime,
                cancellationToken)
            .ConfigureAwait(false);
        if (authenticationResult.IsFailure) {
            return Result.Failure<AuthenticationModel>(authenticationResult.Error);
        }

        UserAuthenticationPrincipalModel principal = authenticationResult.Value;
        await EnsurePasswordSetupReminderAsync(
            principal,
            notificationDeduplicationService,
            notificationWriter,
            cancellationToken).ConfigureAwait(false);

        IssuedAuthenticationTokens tokens = await authenticationTokenService
            .IssueFromPrincipalAsync(principal, cancellationToken, command.ClientContext, command.RememberMe)
            .ConfigureAwait(false);
        return Result.Success(new AuthenticationModel(tokens.AccessToken, tokens.RefreshToken, principal.User));
    }

    private static async Task EnsurePasswordSetupReminderAsync(
        UserAuthenticationPrincipalModel principal,
        INotificationDeduplicationService notificationDeduplicationService,
        INotificationWriter notificationWriter,
        CancellationToken cancellationToken) {
        if (principal.User.HasPassword) {
            return;
        }

        string referenceId = $"password-setup:{principal.UserId.Value}";
        bool exists = await notificationDeduplicationService.ExistsAsync(principal.UserId, NotificationTypes.PasswordSetupSuggested, referenceId, cancellationToken).ConfigureAwait(false);
        if (exists) {
            return;
        }

        var notification = Notification.Create(
            principal.UserId,
            NotificationTypes.PasswordSetupSuggested,
            NotificationPayloads.Empty(),
            referenceId);
        await notificationWriter.AddAsync(notification, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
