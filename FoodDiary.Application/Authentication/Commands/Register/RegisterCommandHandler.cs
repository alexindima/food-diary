using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Authentication;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Common.Utilities;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Contracts.Authentication;
using FoodDiary.Domain.Entities;
using System;

namespace FoodDiary.Application.Authentication.Commands.Register;

public class RegisterCommandHandler : ICommandHandler<RegisterCommand, Result<AuthenticationResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailSender _emailSender;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IPasswordHasher passwordHasher,
        IEmailSender emailSender)
    {
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _passwordHasher = passwordHasher;
        _emailSender = emailSender;
    }

    public async Task<Result<AuthenticationResponse>> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        // Валидация email уникальности через FluentValidation
        var hashedPassword = _passwordHasher.Hash(command.Password);
        var user = User.Create(command.Email, hashedPassword);
        user.UpdateProfile(
            dailyCalorieTarget: 2000,
            proteinTarget: 150,
            fatTarget: 65,
            carbTarget: 200,
            fiberTarget: 28,
            waterGoal: 2000,
            language: "en"
        );

        user = await _userRepository.AddAsync(user);

        var emailToken = SecurityTokenGenerator.GenerateUrlSafeToken();
        var emailTokenHash = _passwordHasher.Hash(emailToken);
        user.SetEmailConfirmationToken(emailTokenHash, DateTime.UtcNow.AddHours(24));

        // Создание токенов
        var roles = Array.Empty<string>();
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email, roles);
        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken(user.Id, user.Email, roles);

        var hashedRefreshToken = _passwordHasher.Hash(refreshToken);
        user.UpdateRefreshToken(hashedRefreshToken);
        await _userRepository.UpdateAsync(user);

        try
        {
            await _emailSender.SendEmailVerificationAsync(
                new EmailVerificationMessage(user.Email, user.Id.Value.ToString(), emailToken),
                cancellationToken);
        }
        catch
        {
            // Email failures should not block registration.
        }

        var userResponse = user.ToResponse();
        var authResponse = new AuthenticationResponse(accessToken, refreshToken, userResponse);
        return Result.Success(authResponse);
    }
}
