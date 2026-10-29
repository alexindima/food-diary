using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;
using FoodDiary.Modules.Identity.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Identity.Presentation.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using HeaderSameSiteMode = Microsoft.Net.Http.Headers.SameSiteMode;

namespace FoodDiary.Modules.Identity.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class BrowserSessionReplacementCookieTests {
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    public void Issuance_PersistsASeparateHostOnlyReplacementMarkerWithoutChangingRememberMe(bool https, bool admin, bool rememberMe) {
        var services = new ServiceCollection();
        services.AddDataProtection();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<BrowserSessionCookieService>();
        using ServiceProvider provider = services.BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = provider };
        context.Request.Scheme = https ? "https" : "http";
        var id = Guid.NewGuid();
        string credential = $"{id:D}.private-authentication-proof";
        var session = new IssuedBrowserSession(credential, id, DateTime.UtcNow.AddDays(30), rememberMe, ["User"], admin);
        BrowserSessionCookieService cookies = provider.GetRequiredService<BrowserSessionCookieService>();

        cookies.Set(context, session);

        string authName = BrowserSessionCookieService.CookieName(context, admin);
        SetCookieHeaderValue[] issued = [.. context.Response.Headers.SetCookie.Select(value => SetCookieHeaderValue.Parse(value!))];
        SetCookieHeaderValue auth = Assert.Single(issued, value => value.Name == authName);
        Assert.Equal(rememberMe, auth.Expires.HasValue);
        SetCookieHeaderValue marker = Assert.Single(issued, value => value.Name == authName + ".previous");
        Assert.Multiple(
            () => Assert.True(marker.Expires > DateTimeOffset.UtcNow.AddDays(300)),
            () => Assert.Equal(https, marker.Secure),
            () => Assert.True(marker.HttpOnly),
            () => Assert.Equal(HeaderSameSiteMode.Lax, marker.SameSite),
            () => Assert.Equal("/", marker.Path.ToString()),
            () => Assert.False(marker.Domain.HasValue),
            () => Assert.DoesNotContain(credential, marker.Value.ToString(), StringComparison.Ordinal),
            () => Assert.DoesNotContain(id.ToString("D"), marker.Value.ToString(), StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Marker_IsBoundToItsRealmAndNeverReadAsAuthentication(bool admin) {
        var cookies = new BrowserSessionCookieService(new EphemeralDataProtectionProvider(), TimeProvider.System);
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        var id = Guid.NewGuid();
        cookies.Set(context, new IssuedBrowserSession("authentication-proof", id, DateTime.UtcNow.AddDays(30), RememberMe: false, ["User"], admin));
        string marker = Assert.Single(context.Response.Headers.SetCookie, value => value!.StartsWith(
            BrowserSessionCookieService.PreviousCookieName(context, admin) + "=", StringComparison.Ordinal))!.Split(';')[0];
        context.Response.Headers.Clear();
        context.Request.Headers.Cookie = marker;
        context.Request.Headers[BrowserSessionCookieService.ClientHeader] = admin ? "admin" : "web";

        Assert.Null(cookies.Read(context));
        Assert.Equal(new RefreshTokenSessionId(id), cookies.ReadPreviousSessionId(context, admin));
        cookies.EnsurePreviousSession(context, id, admin);
        Assert.Equal(0, context.Response.Headers.SetCookie.Count);

        context.Request.Headers.Cookie = BrowserSessionCookieService.PreviousCookieName(context, !admin) + marker[marker.IndexOf('=', StringComparison.Ordinal)..];
        Assert.Null(cookies.ReadPreviousSessionId(context, !admin));
    }

    [Theory]
    [InlineData("malformed")]
    [InlineData("tampered")]
    [InlineData("expired")]
    [InlineData("empty-id")]
    [InlineData("oversized")]
    public void InvalidMarker_IsIgnored(string scenario) {
        var protection = new EphemeralDataProtectionProvider();
        var cookies = new BrowserSessionCookieService(protection, TimeProvider.System);
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        ITimeLimitedDataProtector protector = protection.CreateProtector("FoodDiary.BrowserSessionReplacement.v1",
            BrowserSessionCookieService.CookieName(context, admin: false)).ToTimeLimitedDataProtector();
        string value = scenario switch {
            "expired" => protector.Protect(Guid.NewGuid().ToString("D"), DateTimeOffset.UtcNow.AddDays(-1)),
            "empty-id" => protector.Protect(Guid.Empty.ToString("D"), TimeSpan.FromDays(1)),
            "oversized" => new string('a', 2048),
            "tampered" => protector.Protect(Guid.NewGuid().ToString("D"), TimeSpan.FromDays(1)) + "!",
            _ => "not-a-protected-marker",
        };
        context.Request.Headers.Cookie = BrowserSessionCookieService.PreviousCookieName(context, admin: false) + "=" + value;

        Assert.Null(cookies.ReadPreviousSessionId(context, admin: false));
        Assert.Equal(0, context.Response.Headers.SetCookie.Count);
    }
}
