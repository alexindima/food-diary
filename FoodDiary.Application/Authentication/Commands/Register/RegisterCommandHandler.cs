using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Authentication.Mappings;
using FoodDiary.Application.Authentication.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Authentication.Services;

namespace FoodDiary.Application.Authentication.Commands.Register;

public sealed class RegisterCommandHandler(
    IAuthenticationUserRegistrationService userRegistrationService,
    IPasswordHasher passwordHasher,
    IEmailSender emailSender,
    TimeProvider dateTimeProvider,
    IAuthenticationTokenService authenticationTokenService)
    : ICommandHandler<RegisterCommand, Result<AuthenticationModel>> {

    public async Task<Result<AuthenticationModel>> Handle(RegisterCommand command, CancellationToken cancellationToken) {
        string hashedPassword = passwordHasher.Hash(command.Password);
        var user = User.Create(command.Email, hashedPassword);
        string normalizedLanguage = LanguageCode.FromPreferred(command.Language).Value;
        user.UpdateGoals(new UserGoalUpdate(
            DailyCalorieTarget: 2000,
            ProteinTarget: 150,
            FatTarget: 65,
            CarbTarget: 200,
            FiberTarget: 28,
            WaterGoal: 2000));
        user.SetLanguage(normalizedLanguage);

        user = await userRegistrationService.AddAsync(user, cancellationToken).ConfigureAwait(false);

        string emailToken = SecurityTokenGenerator.GenerateUrlSafeToken();
        string emailTokenHash = passwordHasher.Hash(emailToken);
        user.SetEmailConfirmationToken(new UserTokenIssue(
            TokenHash: emailTokenHash,
            ExpiresAtUtc: dateTimeProvider.GetUtcNow().UtcDateTime.AddHours(24),
            IssuedAtUtc: dateTimeProvider.GetUtcNow().UtcDateTime));

        IssuedAuthenticationTokens tokens = await authenticationTokenService.IssueAndStoreAsync(user, cancellationToken, command.ClientContext).ConfigureAwait(false);

        EmailVerificationMessage message = new(user.Email, user.Id.Value.ToString(), emailToken, user.Language, command.ClientOrigin);
        await emailSender.SendEmailVerificationAsync(message, cancellationToken).ConfigureAwait(false);

        return Result.Success(user.ToAuthenticationModel(tokens));
    }
}
