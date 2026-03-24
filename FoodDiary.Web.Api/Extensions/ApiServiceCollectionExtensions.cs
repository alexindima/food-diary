using System.Text;
using FoodDiary.Application;
using FoodDiary.Infrastructure;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace FoodDiary.Web.Api.Extensions;

public static class ApiServiceCollectionExtensions {
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration) {
        services.AddApplication();
        services.AddInfrastructure(configuration);
        services.AddDistributedMemoryCache();

        var corsOrigins = configuration.GetSection("Cors:Origins").Get<string[]>();
        var allowedOrigins = corsOrigins is { Length: > 0 }
            ? corsOrigins
            : ["http://localhost:4200", "http://localhost:4300"];

        services.AddCors(options => {
            options.AddPolicy(ApiCompositionConstants.CorsPolicyName, policy => {
                policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });

        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"]
                        ?? throw new InvalidOperationException("JWT SecretKey is not configured");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => {
                options.TokenValidationParameters = new TokenValidationParameters {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSettings["Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                };
                options.Events = new JwtBearerEvents {
                    OnMessageReceived = context => {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrWhiteSpace(accessToken) &&
                            path.StartsWithSegments("/hubs/email-verification")) {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    },
                };
            });

        services.AddAuthorization();
        services.Configure<TelegramBotAuthOptions>(configuration.GetSection(TelegramBotAuthOptions.SectionName));
        services.AddPresentationApi();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        return services;
    }
}
