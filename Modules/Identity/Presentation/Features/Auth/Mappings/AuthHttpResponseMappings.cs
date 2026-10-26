using FoodDiary.Modules.Users.Presentation.Mappings.Mappings;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Responses;
using FoodDiary.Modules.Identity.Presentation.Security;
using FoodDiary.Modules.Users.Contracts.Models;
using Microsoft.AspNetCore.Http;

namespace FoodDiary.Modules.Identity.Presentation.Features.Auth.Mappings;

public static class AuthHttpResponseMappings {
    extension(BrowserSessionModel model) {
        public AuthenticationHttpResponse ToHttpResponse() => new(
            AccessToken: null, RefreshToken: string.Empty, User: model.Principal.User.ToHttpResponse(),
            Roles: model.Principal.Roles.ToArray(), SessionId: model.SessionId.ToString("D"), RememberMe: model.RememberMe);
    }

    extension(UserAuthenticationPrincipalModel model) {
        public AuthenticationHttpResponse ToImpersonationHttpResponse(HttpContext context) => new(
            AccessToken: null, RefreshToken: string.Empty, User: model.User.ToHttpResponse(),
            Roles: ImpersonationCookieService.ReadRoles(context), ImpersonationContext: ImpersonationCookieService.ReadContextId(context),
            ImpersonationReason: ImpersonationCookieService.ReadReason(context));
    }

    extension(AuthenticationModel model) {
        public AuthenticationHttpResponse ToHttpResponse() {
            return new AuthenticationHttpResponse(
                model.AccessToken,
                model.RefreshToken,
                model.User.ToHttpResponse(),
                BrowserSession: model.BrowserSession
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
                model.Id.Value,
                model.IsCurrent,
                model.AuthProvider,
                model.Browser,
                model.OperatingSystem,
                model.DeviceType,
                model.CreatedAtUtc,
                model.LastActiveAtUtc);
    }
}
