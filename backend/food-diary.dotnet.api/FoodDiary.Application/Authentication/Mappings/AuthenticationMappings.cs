using FoodDiary.Application.Authentication.Commands.Login;
using FoodDiary.Application.Authentication.Commands.RefreshToken;
using FoodDiary.Application.Authentication.Commands.Register;
using FoodDiary.Contracts.Authentication;

namespace FoodDiary.Application.Authentication.Mappings;

public static class AuthenticationMappings
{
    public static RegisterCommand ToCommand(this RegisterRequest request)
    {
        return new RegisterCommand(request.Email, request.Password);
    }

    public static LoginCommand ToCommand(this LoginRequest request)
    {
        return new LoginCommand(request.Email, request.Password);
    }

    public static RefreshTokenCommand ToCommand(this RefreshTokenRequest request)
    {
        return new RefreshTokenCommand(request.RefreshToken);
    }
}
