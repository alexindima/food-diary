using FoodDiary.Application.Authentication.Abstractions;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Authentication.Mappings;
using FoodDiary.Application.Authentication.Models;
using FoodDiary.Application.Authentication.Services;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Authentication.Commands.GoogleLogin;

public sealed class GoogleLoginCommandHandler(
    IUserRepository userRepository,
    IGoogleTokenValidator googleTokenValidator,
    IPasswordHasher passwordHasher,
    IAuthenticationTokenService authenticationTokenService)
    : ICommandHandler<GoogleLoginCommand, Result<AuthenticationModel>> {
    public async Task<Result<AuthenticationModel>> Handle(GoogleLoginCommand command, CancellationToken cancellationToken) {
        var payloadResult = await googleTokenValidator.ValidateCredentialAsync(command.Credential, cancellationToken);
        if (!payloadResult.IsSuccess) {
            return Result.Failure<AuthenticationModel>(payloadResult.Error);
        }

        var payload = payloadResult.Value;
        var user = await userRepository.GetByEmailIncludingDeletedAsync(payload.Email, cancellationToken);

        if (user is null) {
            user = CreateGoogleUser(payload, passwordHasher);
            user = await userRepository.AddAsync(user, cancellationToken);
        } else {
            var accessError = AuthenticationUserAccessPolicy.EnsureCanAuthenticate(user);
            if (accessError is not null) {
                return Result.Failure<AuthenticationModel>(accessError);
            }

            ApplyGoogleProfile(user, payload);
            await userRepository.UpdateAsync(user, cancellationToken);
        }

        var tokens = await authenticationTokenService.IssueAndStoreAsync(user, cancellationToken);
        return Result.Success(user.ToAuthenticationModel(tokens));
    }

    private static User CreateGoogleUser(GoogleIdentityPayload payload, IPasswordHasher passwordHasher) {
        var placeholderPasswordHash = passwordHasher.Hash(SecurityTokenGenerator.GenerateUrlSafeToken());
        var user = User.Create(payload.Email, placeholderPasswordHash);
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
}
