using FoodDiary.Application.Authentication.Models;
using FoodDiary.Presentation.Api.Features.Auth.Responses;
using FoodDiary.Presentation.Api.Features.Users.Mappings;

namespace FoodDiary.Presentation.Api.Features.Auth.Mappings;

public static class AuthHttpResponseMappings {
    extension(AuthenticationModel model) {
        public AuthenticationHttpResponse ToHttpResponse() {
            return new AuthenticationHttpResponse(
                model.AccessToken,
                model.RefreshToken,
                model.User.ToHttpResponse()
            );
        }
    }

    extension(AdminSsoStartModel model) {
        public AdminSsoStartHttpResponse ToHttpResponse() {
            return new AdminSsoStartHttpResponse(model.Code, model.ExpiresAtUtc);
        }
    }
}
