using FoodDiary.Application.Common.Interfaces.Authentication;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Contracts.Authentication;
using FoodDiary.Contracts.Users;
using FoodDiary.Domain.Entities;

namespace FoodDiary.Application.Services;

public class AuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IPasswordHasher _passwordHasher;

    public AuthenticationService(
        IUserRepository userRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthenticationResponse> RegisterAsync(string email, string password)
    {
        var existingUser = await _userRepository.GetByEmailAsync(email);
        if (existingUser != null)
        {
            throw new Exception("User with this email already exists");
        }

        var hashedPassword = _passwordHasher.Hash(password);
        var user = User.Create(email, hashedPassword);

        user = await _userRepository.AddAsync(user);
        return await CreateAuthenticationResponse(user);
    }

    public async Task<AuthenticationResponse> LoginAsync(string email, string password)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null || !_passwordHasher.Verify(password, user.Password))
        {
            throw new Exception("Invalid credentials");
        }

        return await CreateAuthenticationResponse(user);
    }

    public async Task<string> RefreshTokenAsync(string refreshToken)
    {
        var validationResult = _jwtTokenGenerator.ValidateToken(refreshToken);
        if (validationResult == null)
        {
            throw new Exception("Invalid refresh token");
        }

        var (userId, email) = validationResult.Value;
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null || user.RefreshToken == null)
        {
            throw new Exception("Invalid refresh token");
        }

        if (!_passwordHasher.Verify(refreshToken, user.RefreshToken))
        {
            throw new Exception("Invalid refresh token");
        }

        return _jwtTokenGenerator.GenerateAccessToken(userId, email);
    }

    private async Task<AuthenticationResponse> CreateAuthenticationResponse(User user)
    {
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email);
        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken(user.Id, user.Email);

        var hashedRefreshToken = _passwordHasher.Hash(refreshToken);
        user.UpdateRefreshToken(hashedRefreshToken);
        await _userRepository.UpdateAsync(user);

        var userResponse = new UserResponse(
            user.Email,
            user.Username,
            user.FirstName,
            user.LastName,
            user.BirthDate,
            user.Gender,
            user.Weight,
            user.Height,
            user.ProfileImage,
            user.IsActive
        );

        return new AuthenticationResponse(accessToken, refreshToken, userResponse);
    }
}
