using FoodDiary.Modules.Identity.Presentation.Security;
using FoodDiary.Presentation.Api.Policies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

[ExcludeFromCodeCoverage]
internal static class TestAuthenticationServiceCollectionExtensions {
    internal static IServiceCollection AddSyntheticAuthentication(this IServiceCollection services) {
        // Transport fixtures supply synthetic identities without persisted sessions. Real
        // password, browser and Telegram authorization tests use the unmodified host.
        services.Remove(services.Single(descriptor => descriptor.ServiceType == typeof(IAuthorizationHandler) &&
            descriptor.ImplementationType == typeof(AdminSessionAuthorizationHandler)));
        services.AddAuthorization(options => options.AddPolicy(PresentationPolicyNames.ConfirmAdminCredentialsPolicyName,
            policy => policy.RequireAuthenticatedUser()));
        services.AddAuthentication(options => {
            options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
            options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
            options.DefaultScheme = TestAuthenticationHandler.SchemeName;
        }).AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationHandler.SchemeName, _ => { });
        return services;
    }
}
