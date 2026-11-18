using FoodDiary.Modules.Users.Presentation.Mappings.Mappings;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Responses;
using FoodDiary.Modules.Identity.Presentation.Security;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Presentation.Contracts.Responses;
using Microsoft.AspNetCore.Http;

namespace FoodDiary.Modules.Identity.Presentation.Features.Auth.Mappings;

public static class AuthHttpResponseMappings {
    extension(BrowserSessionModel model) {
        public AuthenticationHttpResponse ToHttpResponse() => new(
            AccessToken: null, RefreshToken: string.Empty, User: ToAuthenticationUser(model.Principal.User),
            Roles: model.Principal.User.MustChangePassword ? [] : model.Principal.Roles.ToArray(),
            SessionId: model.SessionId.ToString("D"), RememberMe: model.RememberMe);
    }

    extension(UserAuthenticationPrincipalModel model) {
        public AuthenticationHttpResponse ToImpersonationHttpResponse(HttpContext context) => new(
            AccessToken: null, RefreshToken: string.Empty, User: ToAuthenticationUser(model.User),
            Roles: ImpersonationCookieService.ReadRoles(context), ImpersonationContext: ImpersonationCookieService.ReadContextId(context),
            ImpersonationReason: ImpersonationCookieService.ReadReason(context));
    }

    extension(AuthenticationModel model) {
        public AuthenticationHttpResponse ToHttpResponse() {
            return new AuthenticationHttpResponse(
                model.AccessToken,
                model.RefreshToken,
                ToAuthenticationUser(model.User),
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

    private static UserHttpResponse ToAuthenticationUser(UserModel user) =>
        !user.MustChangePassword ? user.ToHttpResponse() : new UserHttpResponse(
            Id: user.Id,
            Email: user.Email,
            HasPassword: user.HasPassword,
            Username: null,
            FirstName: null,
            LastName: null,
            BirthDate: null,
            Gender: null,
            WeightKg: null,
            DesiredWeightKg: null,
            DesiredWaistCm: null,
            HeightCm: null,
            ActivityLevel: "Sedentary",
            DailyCalorieTarget: null,
            ProteinTarget: null,
            FatTarget: null,
            CarbTarget: null,
            FiberTarget: null,
            StepGoal: null,
            WaterGoal: null,
            HydrationGoal: null,
            Language: user.Language,
            Theme: null,
            UiStyle: null,
            PushNotificationsEnabled: false,
            FastingPushNotificationsEnabled: false,
            SocialPushNotificationsEnabled: false,
            FastingCheckInReminderHours: 0,
            FastingCheckInFollowUpReminderHours: 0,
            ProfileImage: null,
            ProfileImageAssetId: null,
            DashboardLayout: null,
            IsActive: user.IsActive,
            IsEmailConfirmed: user.IsEmailConfirmed,
            LastLoginAtUtc: null,
            AiConsentAcceptedAt: null,
            MustChangePassword: true,
            HasGoogleIdentity: false);
}
