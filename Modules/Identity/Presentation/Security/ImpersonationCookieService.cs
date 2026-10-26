using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Security.Claims;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Responses;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using FoodDiary.Modules.Identity.Contracts.Authentication.Common;

namespace FoodDiary.Modules.Identity.Presentation.Security;

public sealed class ImpersonationCookieService(IDataProtectionProvider protection, IDistributedCache cache) {
    public const string Scheme = "ImpersonationCookie";
    private const string Prefix = "fooddiary.impersonation.";
    private readonly IDataProtector _protector = protection.CreateProtector("FoodDiary.ImpersonationCookie.v1");

    public static bool TryReadAuthenticatedUserId(HttpContext context, out Guid userId) {
        userId = Guid.Empty;
        return HasContext(context) && context.User.Identity?.IsAuthenticated == true &&
            context.User.HasClaim(JwtImpersonationClaimNames.IsImpersonation, "true") &&
            Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    }

    public static string? ReadReason(HttpContext context) => context.User.FindFirstValue(JwtImpersonationClaimNames.Reason);

    public static string[] ReadRoles(HttpContext context) => [.. context.User.FindAll(ClaimTypes.Role).Select(claim => claim.Value)];

    public static bool HasContext(HttpContext context) =>
        context.Request.Headers.ContainsKey(BrowserSessionCookieService.ContextHeader) ||
        (context.Request.Path.StartsWithSegments("/hubs", StringComparison.OrdinalIgnoreCase) && context.Request.Query.ContainsKey("fd_context"));

    public static string? ReadContextId(HttpContext context) {
        string value = context.Request.Headers[BrowserSessionCookieService.ContextHeader].ToString();
        if (value.Length == 0 && context.Request.Path.StartsWithSegments("/hubs", StringComparison.OrdinalIgnoreCase)) {
            value = context.Request.Query["fd_context"].ToString();
        }
        return value.Length == 32 && Guid.TryParseExact(value, "N", out _) ? value : null;
    }

    public async Task<ExchangeImpersonationHttpResponse> SetAsync(HttpContext context, string accessToken, CancellationToken cancellationToken) {
        string id = Guid.NewGuid().ToString("N");
        string credential = SecurityTokenGenerator.GenerateUrlSafeToken();
        DateTime expires = new JwtSecurityTokenHandler().ReadJwtToken(accessToken).ValidTo;
        await cache.SetStringAsync(CacheKey(id, credential), _protector.Protect(accessToken), new DistributedCacheEntryOptions {
            AbsoluteExpiration = new DateTimeOffset(expires),
        }, cancellationToken).ConfigureAwait(false);
        context.Response.Cookies.Append(CookieName(context, id), credential, Options(context, new DateTimeOffset(expires)));
        return new ExchangeImpersonationHttpResponse(AccessToken: null, ContextId: id);
    }

    public async Task<string?> ReadTokenAsync(HttpContext context, CancellationToken cancellationToken) {
        string? id = ReadContextId(context);
        if (id is null || !context.Request.Cookies.TryGetValue(CookieName(context, id), out string? credential) || credential.Length != 43) {
            return null;
        }
        string? value = await cache.GetStringAsync(CacheKey(id, credential), cancellationToken).ConfigureAwait(false);
        if (value is null) {
            return null;
        }
        try {
            return _protector.Unprotect(value);
        } catch (CryptographicException) {
            return null;
        }
    }

    public async Task DeleteAsync(HttpContext context, CancellationToken cancellationToken) {
        if (ReadContextId(context) is { } id && context.Request.Cookies.TryGetValue(CookieName(context, id), out string? credential)) {
            await cache.RemoveAsync(CacheKey(id, credential), cancellationToken).ConfigureAwait(false);
            context.Response.Cookies.Delete(CookieName(context, id), Options(context, expires: null));
        }
    }

    private static string CacheKey(string id, string credential) => Prefix + id + "." + SecurityTokenGenerator.HashForStorage(credential);
    private static string CookieName(HttpContext context, string id) => (context.Request.IsHttps ? "__Host-" : string.Empty) + Prefix + id;

    private static CookieOptions Options(HttpContext context, DateTimeOffset? expires) => new() {
        HttpOnly = true,
        Secure = context.Request.IsHttps,
        SameSite = SameSiteMode.Lax,
        Path = "/",
        IsEssential = true,
        Expires = expires,
    };
}
