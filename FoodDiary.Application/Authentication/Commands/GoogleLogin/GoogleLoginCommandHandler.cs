using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Authentication.Mappings;
using FoodDiary.Application.Authentication.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Authentication.Services;

namespace FoodDiary.Application.Authentication.Commands.GoogleLogin;

public sealed class GoogleLoginCommandHandler(
    IUserRepository userRepository,
    INotificationRepository notificationRepository,
    IGoogleTokenValidator googleTokenValidator,
    IPasswordHasher passwordHasher,
    IAuthenticationTokenService authenticationTokenService)
    : ICommandHandler<GoogleLoginCommand, Result<AuthenticationModel>> {
    public async Task<Result<AuthenticationModel>> Handle(GoogleLoginCommand command, CancellationToken cancellationToken) {
        var payloadResult = await googleTokenValidator.ValidateCredentialAsync(command.Credential, cancellationToken).ConfigureAwait(false);
        if (!payloadResult.IsSuccess) {
            return Result.Failure<AuthenticationModel>(payloadResult.Error);
        }

        var payload = payloadResult.Value;
        var user = await userRepository.GetByEmailIncludingDeletedAsync(payload.Email, cancellationToken).ConfigureAwait(false);

        if (user is null) {
            user = CreateGoogleUser(payload, passwordHasher);
            user = await userRepository.AddAsync(user, cancellationToken).ConfigureAwait(false);
        } else {
            var accessError = AuthenticationUserAccessPolicy.EnsureCanAuthenticate(user);
            if (accessError is not null) {
                return Result.Failure<AuthenticationModel>(accessError);
            }

            ApplyGoogleProfile(user, payload);
            await userRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        }

        await EnsurePasswordSetupReminderAsync(user, notificationRepository, cancellationToken).ConfigureAwait(false);

        var tokens = await authenticationTokenService
            .IssueAndStoreAsync(user, cancellationToken, command.ClientContext, command.RememberMe)
            .ConfigureAwait(false);
        return Result.Success(user.ToAuthenticationModel(tokens));
    }

    private static User CreateGoogleUser(GoogleIdentityPayload payload, IPasswordHasher passwordHasher) {
        var placeholderPasswordHash = passwordHasher.Hash(SecurityTokenGenerator.GenerateUrlSafeToken());
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
            user.SetEmailConfirmed(true);
        }

        if (!string.IsNullOrWhiteSpace(payload.Locale)) {
            var normalizedLanguage = LanguageCode.FromPreferred(payload.Locale).Value;
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
        INotificationRepository notificationRepository,
        CancellationToken cancellationToken) {
        if (user.HasPassword) {
            return;
        }

        var referenceId = $"password-setup:{user.Id.Value}";
        var exists = await notificationRepository.ExistsAsync(user.Id, NotificationTypes.PasswordSetupSuggested, referenceId, cancellationToken).ConfigureAwait(false);
        if (exists) {
            return;
        }

        var notification = NotificationFactory.CreatePasswordSetupSuggested(user.Id, referenceId);
        await notificationRepository.AddAsync(notification, cancellationToken).ConfigureAwait(false);
    }
}
