using FoodDiary.Modules.Users.Presentation.Mappings.Mappings;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Responses;

namespace FoodDiary.Modules.Identity.Presentation.Features.Auth.Mappings;

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

    extension(ActiveSessionModel model) {
        public ActiveSessionHttpResponse ToHttpResponse() =>
            new(
                model.Id,
                model.IsCurrent,
                model.AuthProvider,
                model.Browser,
                model.OperatingSystem,
                model.DeviceType,
                model.CreatedAtUtc,
                model.LastActiveAtUtc);
    }
}
