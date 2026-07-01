using System.Text;
using FoodDiary.Infrastructure.Options;
using FoodDiary.Web.Api.Build;
using FoodDiary.Web.Api.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;

namespace FoodDiary.Web.Api.Extensions;

public static class ApiAuthenticationServiceCollectionExtensions {
    extension(IServiceCollection services) {
        internal IServiceCollection AddApiAuthentication() {
            services.AddSingleton<IConfigureOptions<Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions>, CorsOptionsSetup>();
            services.AddSingleton<IConfigureOptions<ForwardedHeadersOptions>, ForwardedHeadersOptionsSetup>();
            services.AddSingleton<IConfigureOptions<Microsoft.AspNetCore.RateLimiting.RateLimiterOptions>, RateLimiterOptionsSetup>();
            services.AddSingleton<IConfigureOptions<Microsoft.AspNetCore.OutputCaching.OutputCacheOptions>, OutputCacheOptionsSetup>();
            services.AddSingleton(static serviceProvider => {
                ApiBuildInfoOptions options = serviceProvider.GetRequiredService<IOptions<ApiBuildInfoOptions>>().Value;
                IHostEnvironment environment = serviceProvider.GetRequiredService<IHostEnvironment>();
                TimeProvider timeProvider = serviceProvider.GetRequiredService<TimeProvider>();
                return ApiBuildInfo.Create(options, environment.EnvironmentName, timeProvider);
            });
            services.AddCors(static _ => { });

            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer();
            services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<IOptions<JwtOptions>>(ConfigureJwtBearerOptions);

            services.AddAuthorization();

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
             path.StartsWithSegments("/hubs/notifications", StringComparison.Ordinal))) {
            context.Token = accessToken;
        }
    }
}
