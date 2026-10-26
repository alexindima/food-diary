using FoodDiary.Authentication.Contracts.Options;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;
using FoodDiary.Modules.Identity.Infrastructure.Authentication;
using Microsoft.Extensions.Options;

namespace FoodDiary.Modules.Identity.Infrastructure.Tests;

[ExcludeFromCodeCoverage]
public sealed class AuthenticationSessionLifetimePolicyTests {
    [Theory]
    [InlineData(false, 22, null)]
    [InlineData(false, 23, 53)]
    [InlineData(false, 29, 59)]
    [InlineData(false, 30, null)]
    [InlineData(true, 82, null)]
    [InlineData(true, 83, 173)]
    [InlineData(true, 89, 179)]
    [InlineData(true, 90, null)]
    public void Renewal_UsesOnlyTheLastWeek_AndNeverRevivesAnExpiredSession(bool remember, int elapsedDays, int? renewedDay) {
        DateTime created = new(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        AuthenticationSessionLifetimePolicy policy = CreatePolicy(new BrowserSessionRenewalOptions());
        DateTime? result = policy.GetRenewedExpiration(created, created.AddDays(remember ? 90 : 30), remember, created.AddDays(elapsedDays));
        Assert.Equal(renewedDay is { } day ? created.AddDays(day) : null, result);
    }

    [Fact]
    public void Renewal_ClampsToTheOriginalAbsoluteLimit_OrHonorsExplicitUnlimitedPolicy() {
        DateTime created = new(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        AuthenticationSessionLifetimePolicy capped = CreatePolicy(new BrowserSessionRenewalOptions { AbsoluteLifetimeDays = 180 });
        Assert.Equal(created.AddDays(180), capped.GetRenewedExpiration(created, created.AddDays(179), rememberMe: true, created.AddDays(173)));
        Assert.Null(capped.GetRenewedExpiration(created, created.AddDays(180), rememberMe: true, created.AddDays(179)));
        Assert.Equal(created.AddDays(180), capped.GetAbsoluteExpiration(created));
        AuthenticationSessionLifetimePolicy unlimited = CreatePolicy(new BrowserSessionRenewalOptions { AbsoluteLifetimeDays = null });
        Assert.Null(unlimited.GetAbsoluteExpiration(created));
        Assert.Equal(created.AddDays(263), unlimited.GetRenewedExpiration(created, created.AddDays(179), rememberMe: true, created.AddDays(173)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DefaultPolicy_AllowsContinuedActivityWithoutAnAbsoluteMaximum(bool rememberMe) {
        DateTime created = new(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        AuthenticationSessionLifetimePolicy policy = CreatePolicy(new BrowserSessionRenewalOptions());
        Assert.Null(policy.GetAbsoluteExpiration(created));
        int days = rememberMe ? 90 : 30;
        DateTime expires = created.AddDays(days);
        for (int renewal = 0; renewal < 24; renewal++) {
            DateTime now = expires.AddDays(-6);
            DateTime expected = now.AddDays(days);
            Assert.Equal(expected, policy.GetRenewedExpiration(created, expires, rememberMe, now));
            expires = expected;
        }
        Assert.True(expires > created.AddDays(365));
    }

    private static AuthenticationSessionLifetimePolicy CreatePolicy(BrowserSessionRenewalOptions renewal) => new(
        Options.Create(new JwtOptions { ExpirationMinutes = 60, RefreshTokenExpirationDays = 30, RememberMeRefreshTokenExpirationDays = 90 }),
        Options.Create(renewal));

    [Theory]
    [InlineData(AuthenticationCredentialKind.Browser, false, 43200)]
    [InlineData(AuthenticationCredentialKind.Browser, true, 129600)]
    [InlineData(AuthenticationCredentialKind.AdminBrowser, false, 60)]
    [InlineData(AuthenticationCredentialKind.AdminBrowser, true, 60)]
    public void GetLifetime_UsesConfiguredBrowserAndAdministrativeLimits(AuthenticationCredentialKind kind, bool remember, int minutes) {
        var policy = new AuthenticationSessionLifetimePolicy(Options.Create(new JwtOptions {
            ExpirationMinutes = 60,
            RefreshTokenExpirationDays = 30,
            RememberMeRefreshTokenExpirationDays = 90,
        }), Options.Create(new BrowserSessionRenewalOptions()));
        Assert.Equal(TimeSpan.FromMinutes(minutes), policy.GetLifetime(kind, remember));
        Assert.Throws<ArgumentOutOfRangeException>(() => policy.GetLifetime(AuthenticationCredentialKind.Bearer, rememberMe: false));
    }
}
