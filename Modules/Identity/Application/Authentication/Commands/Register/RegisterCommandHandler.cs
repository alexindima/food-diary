using FoodDiary.Modules.Identity.Contracts.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Services;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.Register;

public sealed class RegisterCommandHandler(
    IUserAuthenticationRegistrationService userRegistrationService,
    IEmailSender emailSender,
    TimeProvider dateTimeProvider,
    IAuthenticationTokenService authenticationTokenService)
    : ICommandHandler<RegisterCommand, Result<AuthenticationModel>> {

    public async Task<Result<AuthenticationModel>> Handle(RegisterCommand command, CancellationToken cancellationToken) {
        string emailToken = SecurityTokenGenerator.GenerateUrlSafeToken();
        DateTime nowUtc = dateTimeProvider.GetUtcNow().UtcDateTime;
        Result<UserAuthenticationPrincipalModel> registrationResult = await userRegistrationService
            .RegisterAsync(
                new UserRegistrationModel(
                    command.Email,
                    command.Password,
                    command.Language,
                    emailToken,
                    nowUtc.AddHours(24),
                    nowUtc),
                cancellationToken)
            .ConfigureAwait(false);
        if (registrationResult.IsFailure) {
            return Result.Failure<AuthenticationModel>(registrationResult.Error);
        }

        UserAuthenticationPrincipalModel principal = registrationResult.Value;
        IssuedAuthenticationTokens tokens = await authenticationTokenService
            .IssueFromPrincipalAsync(principal, cancellationToken, command.ClientContext)
            .ConfigureAwait(false);

        EmailVerificationMessage message = new(
            command.Email.Trim(),
            principal.UserId.Value.ToString(),
            emailToken,
            principal.User.Language,
            command.ClientOrigin);
        await emailSender.SendEmailVerificationAsync(message, cancellationToken).ConfigureAwait(false);

        return Result.Success(new AuthenticationModel(tokens.AccessToken, tokens.RefreshToken, principal.User));
    }
}
