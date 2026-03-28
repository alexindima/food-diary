using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Authentication.Mappings;
using FoodDiary.Application.Authentication.Models;
using FoodDiary.Application.Authentication.Services;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Application.Authentication.Commands.Register;

public class RegisterCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IEmailSender emailSender,
    IDateTimeProvider dateTimeProvider,
    IAuthenticationTokenService authenticationTokenService,
    ILogger<RegisterCommandHandler> logger)
    : ICommandHandler<RegisterCommand, Result<AuthenticationModel>> {

    public async Task<Result<AuthenticationModel>> Handle(RegisterCommand command, CancellationToken cancellationToken) {
        var hashedPassword = passwordHasher.Hash(command.Password);
        var user = User.Create(command.Email, hashedPassword);
        var normalizedLanguage = LanguageCode.FromPreferred(command.Language).Value;
        user.UpdateGoals(new UserGoalUpdate(
            DailyCalorieTarget: 2000,
            ProteinTarget: 150,
            FatTarget: 65,
            CarbTarget: 200,
            FiberTarget: 28,
            WaterGoal: 2000));
        user.SetLanguage(normalizedLanguage);

        user = await userRepository.AddAsync(user, cancellationToken);

        var emailToken = SecurityTokenGenerator.GenerateUrlSafeToken();
        var emailTokenHash = passwordHasher.Hash(emailToken);
        user.SetEmailConfirmationToken(new UserTokenIssue(
            TokenHash: emailTokenHash,
            ExpiresAtUtc: dateTimeProvider.UtcNow.AddHours(24),
            IssuedAtUtc: dateTimeProvider.UtcNow));

        var tokens = await authenticationTokenService.IssueAndStoreAsync(user, cancellationToken);

        try {
            await emailSender.SendEmailVerificationAsync(
                new EmailVerificationMessage(user.Email, user.Id.Value.ToString(), emailToken, user.Language),
                cancellationToken);
        } catch (Exception ex) {
            logger.LogWarning(ex, "Email verification dispatch failed during registration for {Email}", command.Email);
        }

        return Result.Success(user.ToAuthenticationModel(tokens));
    }
}
