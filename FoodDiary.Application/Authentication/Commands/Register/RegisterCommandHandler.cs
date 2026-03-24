using FoodDiary.Application.Authentication.Mappings;
using FoodDiary.Application.Authentication.Models;
using FoodDiary.Application.Authentication.Services;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Common.Utilities;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Authentication.Commands.Register;

public class RegisterCommandHandler : ICommandHandler<RegisterCommand, Result<AuthenticationModel>> {
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailSender _emailSender;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAuthenticationTokenService _authenticationTokenService;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IEmailSender emailSender,
        IDateTimeProvider dateTimeProvider,
        IAuthenticationTokenService authenticationTokenService) {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _emailSender = emailSender;
        _dateTimeProvider = dateTimeProvider;
        _authenticationTokenService = authenticationTokenService;
    }

    public async Task<Result<AuthenticationModel>> Handle(RegisterCommand command, CancellationToken cancellationToken) {
        var hashedPassword = _passwordHasher.Hash(command.Password);
        var user = User.Create(command.Email, hashedPassword);
        var normalizedLanguage = LanguageCode.FromPreferred(command.Language).Value;
        user.UpdateGoals(
            dailyCalorieTarget: 2000,
            proteinTarget: 150,
            fatTarget: 65,
            carbTarget: 200,
            fiberTarget: 28,
            waterGoal: 2000
        );
        user.UpdateProfile(
            language: normalizedLanguage
        );

        user = await _userRepository.AddAsync(user);

        var emailToken = SecurityTokenGenerator.GenerateUrlSafeToken();
        var emailTokenHash = _passwordHasher.Hash(emailToken);
        user.SetEmailConfirmationToken(emailTokenHash, _dateTimeProvider.UtcNow.AddHours(24));

        var tokens = await _authenticationTokenService.IssueAndStoreAsync(user, cancellationToken);

        try {
            await _emailSender.SendEmailVerificationAsync(
                new EmailVerificationMessage(user.Email, user.Id.Value.ToString(), emailToken, user.Language),
                cancellationToken);
        } catch {
        }

        return Result.Success(user.ToAuthenticationModel(tokens));
    }
}
