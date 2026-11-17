using System.Globalization;
using System.Collections.Immutable;
using System.Security.Claims;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Contracts.Authentication.Common;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Responses;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Presentation.Api.Policies;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;
using HttpResults = Microsoft.AspNetCore.Http.Results;

namespace FoodDiary.Modules.Identity.Presentation.Security;

public static class RecipePluginOAuthEndpoints {
    private const string Scheme = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme;
    private const string DecisionField = "fooddiary_decision";

    public static IEndpointRouteBuilder MapRecipePluginOAuth(
        this IEndpointRouteBuilder endpoints, Uri issuer, Uri webOrigin) {
        Uri resource = new(issuer, RecipePluginAccess.ResourcePath);
        endpoints.MapMethods(RecipePluginAccess.AuthorizationPath, ["GET", "POST"],
                (HttpContext context, IAntiforgery antiforgery) =>
                    AuthorizeAsync(context, antiforgery, resource, webOrigin))
            .RequireHost(webOrigin.Authority)
            .WithMetadata(new ConsentEndpoint())
            .ExcludeFromDescription()
            .AllowAnonymous();
        endpoints.MapPost(RecipePluginAccess.TokenPath,
                (HttpContext context, IUserAccessTokenSecurityReader security, IUserAccessTokenSessionReader sessions) =>
                    ExchangeAsync(context, security, sessions, resource))
            .RequireHost(issuer.Authority)
            .ExcludeFromDescription()
            .AllowAnonymous();
        endpoints.MapGet(RecipePluginAccess.ResourceMetadataPath, () => HttpResults.Json(new {
            resource = resource.AbsoluteUri,
            authorization_servers = new[] { issuer.AbsoluteUri },
            scopes_supported = new[] { RecipePluginAccess.RecipeScope, RecipePluginAccess.ShoppingListScope },
        }))
            .RequireHost(issuer.Authority)
            .ExcludeFromDescription()
            .AllowAnonymous();
        return endpoints;
    }

    public static async Task<bool> IsCurrentAsync(
        ClaimsPrincipal principal,
        IUserAccessTokenSecurityReader security,
        IUserAccessTokenSessionReader sessions,
        CancellationToken cancellationToken) {
        return Guid.TryParse(principal.GetClaim(Claims.Subject), out Guid userId) && userId != Guid.Empty &&
               Guid.TryParse(principal.GetClaim(JwtSecurityClaimNames.RefreshSessionId), out Guid sessionId) &&
               sessionId != Guid.Empty &&
               long.TryParse(principal.GetClaim(JwtSecurityClaimNames.SecurityVersion), NumberStyles.None,
                   CultureInfo.InvariantCulture, out long securityVersion) &&
               await security.IsCurrentAsync(userId, securityVersion, cancellationToken) &&
               await sessions.IsActiveAsync(userId, sessionId, cancellationToken);
    }

    private static async Task<IResult> AuthorizeAsync(
        HttpContext context, IAntiforgery antiforgery, Uri resource, Uri webOrigin) {
        if (context.GetOpenIddictServerRequest() is not { } request) {
            return HttpResults.NotFound();
        }
        if (!HasResource(request, resource)) {
            return Reject(Errors.InvalidTarget);
        }

        ImmutableArray<string> scopes = request.GetScopes();
        if (!scopes.Any(IsWriteScope) || scopes.Any(static scope => !IsWriteScope(scope) && scope is not Scopes.OfflineAccess)) {
            return Reject(Errors.InvalidScope);
        }

        AuthenticateResult browser = await context.AuthenticateAsync(BrowserSessionCookieService.Scheme);
        if (!browser.Succeeded || browser.Principal is not { } user ||
            ImpersonationCookieService.HasContext(context) ||
            BrowserSessionCookieService.IsAdmin(context) ||
            !IsOrdinaryBrowserPrincipal(user) ||
            !Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out Guid userId) || userId == Guid.Empty) {
            return HttpMethods.IsGet(context.Request.Method)
                ? ReplyHtml(context, RecipePluginConsentPage.SignIn(webOrigin))
                : Reject(Errors.AccessDenied);
        }

        if (HttpMethods.IsGet(context.Request.Method)) {
            AntiforgeryTokenSet tokens = antiforgery.GetAndStoreTokens(context);
            return ReplyHtml(context, RecipePluginConsentPage.Render(request, tokens, DecisionField));
        }

        try {
            await antiforgery.ValidateRequestAsync(context);
        } catch (AntiforgeryValidationException) {
            return HttpResults.BadRequest();
        }

        IFormCollection form = await context.Request.ReadFormAsync(context.RequestAborted);
        if (form[DecisionField].Count != 1 || form[DecisionField] != "approve") {
            return await CompleteConsentAsync(context, Reject(Errors.AccessDenied), request.RedirectUri);
        }
        var identity = new ClaimsIdentity(Scheme, Claims.Name, Claims.Role);
        identity.SetClaim(Claims.Subject, userId.ToString("D"));
        identity.SetClaim(JwtSecurityClaimNames.RefreshSessionId, user.FindFirstValue(JwtSecurityClaimNames.RefreshSessionId));
        identity.SetClaim(JwtSecurityClaimNames.SecurityVersion, user.FindFirstValue(JwtSecurityClaimNames.SecurityVersion));
        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(scopes);
        principal.SetResources(resource.AbsoluteUri);
        principal.SetDestinations(static _ => [Destinations.AccessToken]);
        return await CompleteConsentAsync(context, HttpResults.SignIn(principal, authenticationScheme: Scheme), request.RedirectUri);
    }

    private static async Task<IResult> CompleteConsentAsync(HttpContext context, IResult authorization, string? registeredRedirect) {
        if (context.Request.Headers["Sec-Fetch-Mode"] != "navigate") {
            return authorization;
        }

        // Start a new navigation so the consent form's CSP does not govern the provider's redirect chain.
        await authorization.ExecuteAsync(context);
        HttpResponse response = context.Response;
        if (response.HasStarted || response.StatusCode != StatusCodes.Status302Found ||
            !Uri.TryCreate(response.Headers.Location.ToString(), UriKind.Absolute, out Uri? destination) ||
            !string.Equals(destination.GetLeftPart(UriPartial.Path), registeredRedirect, StringComparison.Ordinal)) {
            return HttpResults.Empty;
        }

        response.StatusCode = StatusCodes.Status200OK;
        response.ContentLength = null;
        response.Headers.Remove("Location");
        return ReplyHtml(context, RecipePluginConsentPage.Continue(destination), isRedirectPage: true);
    }

    private static async Task<IResult> ExchangeAsync(
        HttpContext context, IUserAccessTokenSecurityReader security, IUserAccessTokenSessionReader sessions, Uri resource) {
        if (context.GetOpenIddictServerRequest() is not { } request) {
            return HttpResults.NotFound();
        }
        if (!HasResource(request, resource)) {
            return Reject(Errors.InvalidTarget);
        }
        AuthenticateResult grant = await context.AuthenticateAsync(Scheme);
        if (!grant.Succeeded || grant.Principal is not { } principal ||
            !principal.HasResource(resource.AbsoluteUri) ||
            !await IsCurrentAsync(principal, security, sessions, context.RequestAborted)) {
            return Reject(Errors.InvalidGrant);
        }
        return HttpResults.SignIn(principal, authenticationScheme: Scheme);
    }

    private static bool IsOrdinaryBrowserPrincipal(ClaimsPrincipal principal) =>
        string.Equals(principal.Identity?.AuthenticationType, BrowserSessionCookieService.Scheme, StringComparison.Ordinal) &&
        principal.HasClaim(static claim => string.Equals(claim.Type, JwtSecurityClaimNames.RefreshSessionId, StringComparison.Ordinal));

    private static bool HasResource(OpenIddictRequest request, Uri resource) =>
        request.GetResources() is [string requested] && string.Equals(requested, resource.AbsoluteUri, StringComparison.Ordinal);

    private static bool IsWriteScope(string scope) =>
        scope is RecipePluginAccess.RecipeScope or RecipePluginAccess.ShoppingListScope;

    private static IResult Reject(string error) => HttpResults.Forbid(new AuthenticationProperties(new Dictionary<string, string?>(StringComparer.Ordinal) {
        [OpenIddictServerAspNetCoreConstants.Properties.Error] = error,
    }), [Scheme]);

    private static IResult ReplyHtml(HttpContext context, string html, bool isRedirectPage = false) {
        context.Response.Headers.CacheControl = "no-store";
        // HTML form submissions need a same-origin Origin header without sending the OAuth query in Referer.
        context.Response.Headers["Referrer-Policy"] = isRedirectPage ? "no-referrer" : "strict-origin";
        string formAction = isRedirectPage ? "'none'" : "'self'";
        context.Response.Headers.ContentSecurityPolicy = $"default-src 'none'; form-action {formAction}; frame-ancestors 'none'";
        return HttpResults.Content(html, "text/html; charset=utf-8");
    }

    public sealed class ConsentEndpoint;
}
