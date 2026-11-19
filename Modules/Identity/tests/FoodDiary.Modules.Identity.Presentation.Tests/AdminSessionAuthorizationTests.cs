using System.Security.Claims;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Authentication.Services;
using FoodDiary.Modules.Identity.Domain.Entities.Users;
using FoodDiary.Modules.Identity.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Identity.Presentation.Extensions;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Policies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Identity.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class AdminSessionAuthorizationTests {
    [Theory]
    [InlineData("ordinary", true)]
    [InlineData("admin-password", true)]
    [InlineData("admin-sso", false)]
    [InlineData("missing-session", false)]
    public async Task CredentialChangesRequireFreshDirectConfirmationForAdministrators(string scenario, bool expected) {
        var user = UserId.New();
        DateTime now = DateTime.UtcNow;
        var session = UserRefreshTokenSession.Create(RefreshTokenSessionId.New(), user, "proof", rememberMe: false,
            "password", ipAddress: null, userAgent: null, now, now.AddHours(1));
        if (scenario is "admin-sso") {
            session.GrantAdminAccess(now.AddMinutes(10));
        }
        IRefreshTokenSessionReadRepository sessions = Substitute.For<IRefreshTokenSessionReadRepository>();
        sessions.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdentityPresentation();
        services.AddSingleton(sessions);
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<PrivilegedSessionService>();
        await using ServiceProvider provider = services.BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        var identity = new ClaimsIdentity("test", ClaimTypes.Name, ClaimTypes.Role);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Value.ToString("D")));
        identity.AddClaim(new Claim(ClaimTypes.Role, scenario is "ordinary" ? "User" : RoleNames.Admin));
        if (scenario is not "missing-session") {
            identity.AddClaim(new Claim(JwtSecurityClaimNames.RefreshSessionId, session.Id.Value.ToString("D")));
        }

        AuthorizationResult result = await scope.ServiceProvider.GetRequiredService<IAuthorizationService>()
            .AuthorizeAsync(new ClaimsPrincipal(identity), new DefaultHttpContext(), PresentationPolicyNames.ConfirmAdminCredentialsPolicyName);

        Assert.Equal(expected, result.Succeeded);
        if (scenario is "ordinary" or "missing-session") {
            Assert.Empty(sessions.ReceivedCalls());
        }
    }
}
