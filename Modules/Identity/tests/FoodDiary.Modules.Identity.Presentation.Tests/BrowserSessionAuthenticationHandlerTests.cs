using FoodDiary.Authentication.Contracts.Options;
using FoodDiary.Mediator;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Modules.Identity.Contracts.Authentication.Common;
using FoodDiary.Modules.Identity.Presentation.Security;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Tests;
using FoodDiary.Results;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace FoodDiary.Modules.Identity.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class BrowserSessionAuthenticationHandlerTests {
    [Theory]
    [InlineData(20, 90, 20)]
    [InlineData(90, 15, 15)]
    [InlineData(null, 90, 60)]
    public async Task AuthenticationTicket_CannotOutliveSessionPrivilegeCapOrDefaultConnectionLifetime(int? capMinutes, int sessionMinutes, int expectedMinutes) {
        DateTime now = new(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var id = Guid.NewGuid();
        var userId = UserId.New();
        var principal = new UserSessionPrincipalModel(userId, "session@example.com", ["Premium"],
            capMinutes.HasValue ? now.AddMinutes(capMinutes.Value) : null, SecurityVersion: 7);
        var session = new BrowserSessionIdentityModel(id, now.AddMinutes(sessionMinutes), principal);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IOptions<JwtOptions>>(OptionsFactory.Create(new JwtOptions { ExpirationMinutes = 60 }));
        services.AddSingleton<TimeProvider>(new FixedClock(new DateTimeOffset(now)));
        services.AddSingleton(new BrowserSessionCookieService());
        services.AddSingleton<ISender>(SubstituteSender.Create(Result.Success(session)));
        services.AddAuthentication(BrowserSessionCookieService.Scheme)
            .AddScheme<AuthenticationSchemeOptions, BrowserSessionAuthenticationHandler>(BrowserSessionCookieService.Scheme, _ => { });
        await using ServiceProvider provider = services.BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = provider };
        context.Request.Headers.Cookie = BrowserSessionCookieService.BrowserCookieName + "=" + BrowserSessionCredential.Create(id);
        context.Request.Headers["X-Test-Role"] = "Admin";
        AuthenticateResult result = await context.AuthenticateAsync(BrowserSessionCookieService.Scheme);
        Assert.True(result.Succeeded);
        Assert.Equal(new DateTimeOffset(now.AddMinutes(expectedMinutes)), result.Properties!.ExpiresUtc);
        Assert.True(result.Principal!.IsInRole("Premium"));
        Assert.False(result.Principal.IsInRole("Admin"));
        Assert.Equal(userId.Value.ToString("D"), result.Principal.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedClock(DateTimeOffset now) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
