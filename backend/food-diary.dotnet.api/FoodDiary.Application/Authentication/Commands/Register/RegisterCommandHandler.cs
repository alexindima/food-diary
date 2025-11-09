using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Authentication;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Contracts.Authentication;
using FoodDiary.Domain.Entities;

namespace FoodDiary.Application.Authentication.Commands.Register;

public class RegisterCommandHandler : ICommandHandler<RegisterCommand, Result<AuthenticationResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<AuthenticationResponse>> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        // Валидация email уникальности через FluentValidation
        var hashedPassword = _passwordHasher.Hash(command.Password);
        var user = User.Create(command.Email, hashedPassword);

        user = await _userRepository.AddAsync(user);

        // Создание токенов
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email);
        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken(user.Id, user.Email);

        var hashedRefreshToken = _passwordHasher.Hash(refreshToken);
        user.UpdateRefreshToken(hashedRefreshToken);
        await _userRepository.UpdateAsync(user);

        var userResponse = user.ToResponse();
        var authResponse = new AuthenticationResponse(accessToken, refreshToken, userResponse);
        return Result.Success(authResponse);
    }
}
