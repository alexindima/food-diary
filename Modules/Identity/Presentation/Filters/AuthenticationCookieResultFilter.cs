using FoodDiary.Modules.Identity.Presentation.Security;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using FoodDiary.Mediator;
using FoodDiary.Modules.Identity.Application.Authentication.Commands.CreateBrowserSession;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Results;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;

namespace FoodDiary.Modules.Identity.Presentation.Filters;

public sealed class AuthenticationCookieResultFilter(
    RefreshTokenCookieService refreshTokenCookies,
    BrowserSessionCookieService browserCookies,
    ISender sender) : IAsyncResultFilter {
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next) {
        if (context.Result is ObjectResult { Value: AuthenticationHttpResponse { BrowserSession: { } issued } direct } directResponse) {
            SetIssuedSession(context.HttpContext, directResponse, direct, issued);
        } else if (context.Result is ObjectResult { Value: AuthenticationHttpResponse authentication } response &&
            !string.IsNullOrEmpty(authentication.RefreshToken)) {
            string path = context.HttpContext.Request.Path.Value ?? string.Empty;
            bool bot = path.EndsWith("/telegram/bot/auth", StringComparison.OrdinalIgnoreCase);
            bool admin = path.EndsWith("/admin-sso/exchange", StringComparison.OrdinalIgnoreCase);
            bool telegram = context.HttpContext.Request.Headers[BrowserSessionCookieService.ClientHeader] == "telegram" &&
                path.Contains("/auth/", StringComparison.OrdinalIgnoreCase) && !admin;
            if (bot || telegram) {
                if (!bot && !context.Filters.OfType<SuppressRefreshTokenCookieAttribute>().Any()) {
                    refreshTokenCookies.Set(context.HttpContext, authentication.RefreshToken);
                }
            } else {
                DateTime? maximumExpires = admin
                    ? new JwtSecurityTokenHandler().ReadJwtToken(authentication.AccessToken ??
                        throw new InvalidOperationException("Administrative sign-in did not return an internal ticket.")).ValidTo
                    : null;
                string? existing = context.HttpContext.Request.Cookies[BrowserSessionCookieService.CookieName(context.HttpContext, admin)];
                Result<BrowserSessionModel> session = await sender.Send(
                    new CreateBrowserSessionCommand(authentication.RefreshToken, existing, maximumExpires), context.HttpContext.RequestAborted).ConfigureAwait(false);
                if (session.IsFailure) {
                    context.Result = new UnauthorizedResult();
                } else {
                    browserCookies.Set(context.HttpContext, session.Value, admin);
                    if (!admin && context.HttpContext.Request.Cookies.ContainsKey(RefreshTokenCookieService.CookieName)) {
                        refreshTokenCookies.Delete(context.HttpContext);
                    }
                    response.Value = authentication with {
                        AccessToken = null,
                        RefreshToken = string.Empty,
                        Roles = session.Value.Principal.Roles.ToArray(),
                        SessionId = session.Value.SessionId.ToString("D"),
                        RememberMe = session.Value.RememberMe,
                    };
                }
            }
        }

        await next().ConfigureAwait(false);
    }

    private void SetIssuedSession(HttpContext context, ObjectResult response, AuthenticationHttpResponse authentication, IssuedBrowserSession session) {
        browserCookies.Set(context, session);
        if (!session.IsAdmin && context.Request.Cookies.ContainsKey(RefreshTokenCookieService.CookieName)) {
            refreshTokenCookies.Delete(context);
        }
        response.Value = authentication with {
            AccessToken = null,
            RefreshToken = string.Empty,
            Roles = session.Roles.ToArray(),
            SessionId = session.SessionId.ToString("D"),
            RememberMe = session.RememberMe,
        };
    }
}
