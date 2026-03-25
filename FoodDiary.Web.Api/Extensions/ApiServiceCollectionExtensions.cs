using System.Text;
using System.Threading.RateLimiting;
using FoodDiary.Application;
using FoodDiary.Infrastructure;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Options;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Web.Api.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

namespace FoodDiary.Web.Api.Extensions;

public static class ApiServiceCollectionExtensions {
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration) {
        services.AddApplication();
        services.AddInfrastructure(configuration);
        services.AddDistributedMemoryCache();
        services
            .AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services
            .AddOptions<TelegramBotAuthOptions>()
            .BindConfiguration(TelegramBotAuthOptions.SectionName)
            .Validate(static options => string.IsNullOrWhiteSpace(options.ApiSecret) || options.ApiSecret.Length >= 16,
                "TelegramBot:ApiSecret must be empty or at least 16 characters long.")
            .ValidateOnStart();

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

        var jwtOptions = configuration.GetRequiredSection(JwtOptions.SectionName).Get<JwtOptions>()
                         ?? throw new InvalidOperationException("JWT settings are not configured.");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => {
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
        services.AddProblemDetails();
        services.AddExceptionHandler<ApiExceptionHandler>();
        services.AddRateLimiter(options => {
            options.OnRejected = async (context, cancellationToken) => {
                var httpContext = context.HttpContext;
                httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                await httpContext.Response.WriteAsJsonAsync(new ApiErrorHttpResponse(
                    "RateLimit.Exceeded",
                    "Too many requests. Try again later.",
                    httpContext.TraceIdentifier), cancellationToken);
            };
            options.AddPolicy<string>(PresentationPolicyNames.AuthRateLimitPolicyName, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: $"auth:{GetRateLimitPartitionKey(context)}",
                    factory: static _ => new FixedWindowRateLimiterOptions {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        AutoReplenishment = true,
                    }));
            options.AddPolicy<string>(PresentationPolicyNames.AiRateLimitPolicyName, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: $"ai:{GetRateLimitPartitionKey(context)}",
                    factory: static _ => new FixedWindowRateLimiterOptions {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        AutoReplenishment = true,
                    }));
        });
        services.AddOutputCache(options => {
            options.AddPolicy(PresentationPolicyNames.AdminAiUsageCachePolicyName, builder => builder
                .Cache()
                .Expire(TimeSpan.FromSeconds(15))
                .SetVaryByQuery("*")
                .Tag("admin-ai-usage"));
        });
        services.AddPresentationApi();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options => {
            options.SwaggerDoc("v1", new OpenApiInfo {
                Title = "FoodDiary API",
                Version = "v1",
            });
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme.",
            });
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement {
                [new OpenApiSecuritySchemeReference("Bearer", document, null)] = []
            });
        });

        return services;
    }

    private static string GetRateLimitPartitionKey(HttpContext context) {
        var userId = context.User.GetUserGuid();
        if (userId.HasValue && userId.Value != Guid.Empty) {
            return $"user:{userId.Value:D}";
        }

        var forwardedFor = context.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(forwardedFor)) {
            return $"ip:{forwardedFor}";
        }

        var remoteIp = context.Connection.RemoteIpAddress?.ToString();
        return $"ip:{remoteIp ?? "unknown"}";
    }
}
