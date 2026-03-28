using FoodDiary.Application.Authentication.Models;
using FoodDiary.Presentation.Api.Features.Auth.Responses;
using FoodDiary.Presentation.Api.Features.Users.Mappings;

namespace FoodDiary.Presentation.Api.Features.Auth.Mappings;

public static class AuthHttpResponseMappings {
    public static AuthenticationHttpResponse ToHttpResponse(this AuthenticationModel model) {
        return new AuthenticationHttpResponse(
            model.AccessToken,
            model.RefreshToken,
            model.User.ToHttpResponse()
        );
    }

    public static AdminSsoStartHttpResponse ToHttpResponse(this AdminSsoStartModel model) {
        return new AdminSsoStartHttpResponse(model.Code, model.ExpiresAtUtc);
    }
}
