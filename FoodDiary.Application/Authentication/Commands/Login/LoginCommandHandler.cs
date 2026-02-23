using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Authentication.Services;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Contracts.Authentication;

namespace FoodDiary.Application.Authentication.Commands.Login;

public class LoginCommandHandler : ICommandHandler<LoginCommand, Result<AuthenticationResponse>> {
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuthenticationTokenService _authenticationTokenService;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IAuthenticationTokenService authenticationTokenService) {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _authenticationTokenService = authenticationTokenService;
    }

    public async Task<Result<AuthenticationResponse>> Handle(LoginCommand command, CancellationToken cancellationToken) {
        var user = await _userRepository.GetByEmailIncludingDeletedAsync(command.Email);
        if (user == null || !_passwordHasher.Verify(command.Password, user.Password)) {
            return Result.Failure<AuthenticationResponse>(Errors.Authentication.InvalidCredentials);
        }

        if (user.DeletedAt is not null) {
            return Result.Failure<AuthenticationResponse>(Errors.Authentication.AccountDeleted);
        }

        if (!user.IsActive) {
            return Result.Failure<AuthenticationResponse>(Errors.Authentication.InvalidCredentials);
        }

        var tokens = await _authenticationTokenService.IssueAndStoreAsync(user, cancellationToken);

        var userResponse = user.ToResponse();
        var authResponse = new AuthenticationResponse(tokens.AccessToken, tokens.RefreshToken, userResponse);
        return Result.Success(authResponse);
    }
}
