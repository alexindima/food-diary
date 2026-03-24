using FoodDiary.Application.Authentication.Models;
using FoodDiary.Application.Authentication.Services;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Authentication.Mappings;

public static class AuthenticationMappings {
    public static AuthenticationModel ToAuthenticationModel(this User user, IssuedAuthenticationTokens tokens) {
        return new AuthenticationModel(tokens.AccessToken, tokens.RefreshToken, user.ToModel());
    }
}
