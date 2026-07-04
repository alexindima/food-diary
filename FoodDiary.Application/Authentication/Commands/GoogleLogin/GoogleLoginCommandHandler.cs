using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Authentication.Mappings;
using FoodDiary.Application.Authentication.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Authentication.Services;
using FoodDiary.Domain.Entities.Notifications;

namespace FoodDiary.Application.Authentication.Commands.GoogleLogin;

public sealed class GoogleLoginCommandHandler(
    IAuthenticationUserMutationService userMutationService,
    INotificationReadRepository notificationRepository,
    INotificationWriter notificationWriter,
    IGoogleTokenValidator googleTokenValidator,
    IPasswordHasher passwordHasher,
    IAuthenticationTokenService authenticationTokenService)
    : ICommandHandler<GoogleLoginCommand, Result<AuthenticationModel>> {
    public async Task<Result<AuthenticationModel>> Handle(GoogleLoginCommand command, CancellationToken cancellationToken) {
        Result<GoogleIdentityPayload> payloadResult = await googleTokenValidator.ValidateCredentialAsync(command.Credential, cancellationToken).ConfigureAwait(false);
        if (!payloadResult.IsSuccess) {
            return Result.Failure<AuthenticationModel>(payloadResult.Error);
        }

        GoogleIdentityPayload payload = payloadResult.Value;
        User? user = await userMutationService.GetByEmailIncludingDeletedAsync(payload.Email, cancellationToken).ConfigureAwait(false);

        if (user is null) {
            user = CreateGoogleUser(payload, passwordHasher);
            user = await userMutationService.AddAsync(user, cancellationToken).ConfigureAwait(false);
        } else {
            Error? accessError = AuthenticationUserAccessPolicy.EnsureCanAuthenticate(user);
            if (accessError is not null) {
                return Result.Failure<AuthenticationModel>(accessError);
            }

            ApplyGoogleProfile(user, payload);
            await userMutationService.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        }

        await EnsurePasswordSetupReminderAsync(user, notificationRepository, notificationWriter, cancellationToken).ConfigureAwait(false);

        IssuedAuthenticationTokens tokens = await authenticationTokenService
            .IssueAndStoreAsync(user, cancellationToken, command.ClientContext, command.RememberMe)
            .ConfigureAwait(false);
        return Result.Success(user.ToAuthenticationModel(tokens));
    }

    private static User CreateGoogleUser(GoogleIdentityPayload payload, IPasswordHasher passwordHasher) {
        string placeholderPasswordHash = passwordHasher.Hash(SecurityTokenGenerator.GenerateUrlSafeToken());
        var user = User.Create(payload.Email, placeholderPasswordHash, hasPassword: false);
        user.UpdateGoals(new UserGoalUpdate(
            DailyCalorieTarget: 2000,
            ProteinTarget: 150,
            FatTarget: 65,
            CarbTarget: 200,
            FiberTarget: 28,
            WaterGoal: 2000));
        ApplyGoogleProfile(user, payload);
        return user;
    }

    private static void ApplyGoogleProfile(User user, GoogleIdentityPayload payload) {
        if (!user.IsEmailConfirmed) {
            user.SetEmailConfirmed(isConfirmed: true);
        }

        if (!string.IsNullOrWhiteSpace(payload.Locale)) {
            string normalizedLanguage = LanguageCode.FromPreferred(payload.Locale).Value;
            if (!string.Equals(user.Language, normalizedLanguage, StringComparison.Ordinal)) {
                user.SetLanguage(normalizedLanguage);
            }
        }

        user.UpdatePersonalInfo(
            firstName: payload.FirstName,
            lastName: payload.LastName);
    }

    private static async Task EnsurePasswordSetupReminderAsync(
        User user,
        INotificationReadRepository notificationRepository,
        INotificationWriter notificationWriter,
        CancellationToken cancellationToken) {
        if (user.HasPassword) {
            return;
        }

        string referenceId = $"password-setup:{user.Id.Value}";
        bool exists = await notificationRepository.ExistsAsync(user.Id, NotificationTypes.PasswordSetupSuggested, referenceId, cancellationToken).ConfigureAwait(false);
        if (exists) {
            return;
        }

        Notification notification = NotificationFactory.CreatePasswordSetupSuggested(user.Id, referenceId);
        await notificationWriter.AddAsync(notification, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
