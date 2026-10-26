using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using FoodDiary.Modules.Identity.Contracts.Authentication.Common;
using FoodDiary.Authentication.Contracts.Options;
using System.Text;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Presentation.Api.Features.Version;
using FoodDiary.Web.Api.Build;
using FoodDiary.Web.Api.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using FoodDiary.Modules.Identity.Presentation.Security;
using Microsoft.AspNetCore.Authentication;

namespace FoodDiary.Web.Api.Extensions;

public static class ApiAuthenticationServiceCollectionExtensions {
    extension(IServiceCollection services) {
        internal IServiceCollection AddApiAuthentication() {
            services.AddSingleton<IConfigureOptions<CorsOptions>, CorsOptionsSetup>();
            services.AddSingleton<IConfigureOptions<ForwardedHeadersOptions>, ForwardedHeadersOptionsSetup>();
            services.AddSingleton<IConfigureOptions<RateLimiterOptions>, RateLimiterOptionsSetup>();
            services.AddSingleton<IConfigureOptions<OutputCacheOptions>, OutputCacheOptionsSetup>();
            services.AddSingleton(static serviceProvider => {
                ApiBuildInfoOptions options = serviceProvider.GetRequiredService<IOptions<ApiBuildInfoOptions>>().Value;
                IHostEnvironment environment = serviceProvider.GetRequiredService<IHostEnvironment>();
                TimeProvider timeProvider = serviceProvider.GetRequiredService<TimeProvider>();
                return ApiBuildInfo.Create(options, environment.EnvironmentName, timeProvider);
            });
            services.AddSingleton<IApiVersionInfo>(static serviceProvider =>
                serviceProvider.GetRequiredService<ApiBuildInfo>());
            services.AddCors(static _ => { });

            services
                .AddAuthentication("FoodDiary")
                .AddPolicyScheme("FoodDiary", "FoodDiary client authentication", options => {
                    options.ForwardDefaultSelector = context => {
                        if (context.Request.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ||
                            context.Request.Headers[BrowserSessionCookieService.ClientHeader] == "telegram" ||
                            (context.Request.Path.StartsWithSegments("/hubs", StringComparison.OrdinalIgnoreCase) && context.Request.Query.ContainsKey("access_token"))) {
                            return JwtBearerDefaults.AuthenticationScheme;
                        }
                        return ImpersonationCookieService.HasContext(context) ? ImpersonationCookieService.Scheme : BrowserSessionCookieService.Scheme;
                    };
                })
                .AddJwtBearer()
                .AddJwtBearer(ImpersonationCookieService.Scheme, static _ => { })
                .AddScheme<AuthenticationSchemeOptions, BrowserSessionAuthenticationHandler>(BrowserSessionCookieService.Scheme, static _ => { });
            services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<IOptions<JwtOptions>>(ConfigureJwtBearerOptions);
            services.AddOptions<JwtBearerOptions>(ImpersonationCookieService.Scheme)
                .Configure<IOptions<JwtOptions>>(ConfigureJwtBearerOptions)
                .PostConfigure(options => options.Events.OnMessageReceived = async context => {
                    context.Token = await context.HttpContext.RequestServices.GetRequiredService<ImpersonationCookieService>()
                        .ReadTokenAsync(context.HttpContext, context.HttpContext.RequestAborted).ConfigureAwait(false);
                    if (context.Token is null) {
                        context.Fail("The impersonation context is invalid or expired.");
                    }
                });

            services.AddAuthorization();
            services.AddSingleton<IAuthorizationMiddlewareResultHandler, ApiAuthorizationMiddlewareResultHandler>();

            return services;
        }
    }

    private static void ConfigureJwtBearerOptions(JwtBearerOptions options, IOptions<JwtOptions> jwtOptionsAccessor) {
        JwtOptions jwtOptions = jwtOptionsAccessor.Value;
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };
        options.Events = new JwtBearerEvents {
            OnTokenValidated = async context => {
                if (!JwtTokenUseValidator.IsAccessToken(context.Principal)) {
                    context.Fail("The token is not an access token.");
                    return;
                }

                IUserAccessTokenSecurityReader securityReader = context.HttpContext.RequestServices
                    .GetRequiredService<IUserAccessTokenSecurityReader>();
                bool isCurrent = await AccessTokenSecurityStateValidator
                    .IsCurrentAsync(context.Principal, securityReader,
                        context.HttpContext.RequestServices.GetRequiredService<IUserAccessTokenSessionReader>(),
                        context.HttpContext.RequestAborted)
                    .ConfigureAwait(false);
                if (!isCurrent) {
                    context.Fail("The token is no longer valid for the current account security state.");
                }
            },
            OnMessageReceived = context => {
                ExtractSignalRAccessToken(context);
                return Task.CompletedTask;
            },
        };
    }

    private static void ExtractSignalRAccessToken(MessageReceivedContext context) {
        StringValues accessToken = context.Request.Query["access_token"];
        PathString path = context.HttpContext.Request.Path;
        if (!string.IsNullOrWhiteSpace(accessToken) &&
            (path.StartsWithSegments("/hubs/email-verification", StringComparison.Ordinal) ||
             path.StartsWithSegments("/hubs/notifications", StringComparison.Ordinal) ||
             path.StartsWithSegments("/hubs/food-recognition", StringComparison.Ordinal))) {
            context.Token = accessToken;
        }
    }
}
