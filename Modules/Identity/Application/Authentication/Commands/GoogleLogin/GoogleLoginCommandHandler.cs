using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Services;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Notifications.Contracts.Common;
using FoodDiary.Modules.Identity.Contracts.Errors;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.GoogleLogin;

public sealed class GoogleLoginCommandHandler(
    IUserAuthenticationIdentityService userIdentityService,
    INotificationDeduplicationService notificationDeduplicationService,
    INotificationWriter notificationWriter,
    IGoogleTokenValidator googleTokenValidator,
    TimeProvider dateTimeProvider,
    IAuthenticationTokenService authenticationTokenService)
    : ICommandHandler<GoogleLoginCommand, Result<AuthenticationModel>> {
    public async Task<Result<AuthenticationModel>> Handle(GoogleLoginCommand command, CancellationToken cancellationToken) {
        Result<GoogleIdentityPayload> payloadResult = await googleTokenValidator.ValidateCredentialAsync(command.Credential, cancellationToken);
        if (!payloadResult.IsSuccess) {
            return Result.Failure<AuthenticationModel>(payloadResult.Error);
        }

        GoogleIdentityPayload payload = payloadResult.Value;
        var identity = new UserGoogleAuthenticationModel(payload.Issuer, payload.Subject, payload.Email,
            payload.FirstName, payload.LastName, payload.Locale);
        DateTime now = dateTimeProvider.GetUtcNow().UtcDateTime;
        bool admin = command.ClientContext?.CredentialKind == AuthenticationCredentialKind.AdminBrowser;
        Result<UserAuthenticationPrincipalModel> authenticationResult = await (admin
            ? userIdentityService.AuthenticateLinkedGoogleAsync(identity, now, cancellationToken)
            : userIdentityService.AuthenticateGoogleAsync(identity, now, cancellationToken));
        if (authenticationResult.IsFailure) {
            return Result.Failure<AuthenticationModel>(authenticationResult.Error);
        }

        UserAuthenticationPrincipalModel principal = authenticationResult.Value;
        if (command.ExpectedUserId is { } expectedUserId && principal.UserId.Value != expectedUserId) {
            return Result.Failure<AuthenticationModel>(IdentityErrors.ReauthenticationAccountMismatch);
        }
        if (admin) {
            if (!principal.Roles.Contains(RoleNames.Admin, StringComparer.Ordinal)) {
                return Result.Failure<AuthenticationModel>(IdentityErrors.AdminSsoForbidden);
            }
        } else {
            await EnsurePasswordSetupReminderAsync(principal, notificationDeduplicationService, notificationWriter, cancellationToken);
        }

        IssuedAuthenticationTokens tokens = await authenticationTokenService
            .IssueFromPrincipalAsync(principal, cancellationToken, command.ClientContext, command.RememberMe);
        return Result.Success(new AuthenticationModel(tokens.AccessToken, tokens.RefreshToken, principal.User, tokens.BrowserSession));
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
        bool exists = await notificationDeduplicationService.ExistsAsync(principal.UserId, NotificationTypes.PasswordSetupSuggested, referenceId, cancellationToken);
        if (exists) {
            return;
        }

        var notification = new NotificationRequest(
            principal.UserId,
            NotificationIntent.PasswordSetupSuggested(referenceId));
        await notificationWriter.AddAsync(notification, cancellationToken: cancellationToken);
    }
}
