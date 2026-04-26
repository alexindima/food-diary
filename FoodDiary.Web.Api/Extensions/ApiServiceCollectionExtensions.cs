using System.Text;
using FoodDiary.Application;
using FoodDiary.Integrations;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Infrastructure;
using FoodDiary.Infrastructure.Options;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Web.Api.Build;
using FoodDiary.Presentation.Api.Options;
using FoodDiary.Resources.Notifications;
using FoodDiary.Web.Api.HealthChecks;
using FoodDiary.Web.Api.Options;
using FoodDiary.Web.Api.Services;
using OpenTelemetry;
using FoodDiary.Web.Api.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace FoodDiary.Web.Api.Extensions;

public static class ApiServiceCollectionExtensions {
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration) {
        services.AddApplication();
        services.AddInfrastructure(configuration);
        services.AddIntegrations(configuration);
        services.AddSingleton<INotificationTextRenderer, NotificationResourceRenderer>();
        services.AddDistributedMemoryCache();
        services
            .AddOptions<ApiCorsOptions>()
            .BindConfiguration(ApiCorsOptions.SectionName)
            .Validate(ApiCorsOptions.HasValidOrigins,
                "Cors:Origins must contain at least one absolute origin URL.")
            .ValidateOnStart();
        services
            .AddOptions<ApiForwardedHeadersOptions>()
            .BindConfiguration(ApiForwardedHeadersOptions.SectionName)
            .Validate(ApiForwardedHeadersOptions.HasValidForwardLimit,
                "ForwardedHeaders:ForwardLimit must be greater than zero.")
            .Validate(ApiForwardedHeadersOptions.HasValidKnownProxies,
                "ForwardedHeaders:KnownProxies must contain valid IP addresses.")
            .Validate(ApiForwardedHeadersOptions.HasValidKnownNetworks,
                "ForwardedHeaders:KnownNetworks must contain valid CIDR entries.")
            .ValidateOnStart();
        services
            .AddOptions<ApiRateLimitingOptions>()
            .BindConfiguration(ApiRateLimitingOptions.SectionName)
            .Validate(ApiRateLimitingOptions.HasValidAuth,
                "RateLimiting:Auth requires positive PermitLimit/WindowSeconds and non-negative QueueLimit.")
            .Validate(ApiRateLimitingOptions.HasValidAi,
                "RateLimiting:Ai requires positive PermitLimit/WindowSeconds and non-negative QueueLimit.")
            .ValidateOnStart();
        services
            .AddOptions<ApiOutputCacheOptions>()
            .BindConfiguration(ApiOutputCacheOptions.SectionName)
            .Validate(ApiOutputCacheOptions.HasValidAdminAiUsage,
                "OutputCache:AdminAiUsage:ExpirationSeconds must be greater than zero.")
            .ValidateOnStart();
        services
            .AddOptions<OpenTelemetryOptions>()
            .BindConfiguration(OpenTelemetryOptions.SectionName)
            .Validate(OpenTelemetryOptions.HasValidOtlpEndpoint,
                "OpenTelemetry:Otlp:Endpoint must be a valid absolute URI when provided.")
            .ValidateOnStart();
        services
            .AddOptions<TelegramBotAuthOptions>()
            .BindConfiguration(TelegramBotAuthOptions.SectionName)
            .Validate(TelegramBotAuthOptions.HasValidApiSecret,
                "TelegramBot:ApiSecret must be empty or at least 16 characters long.")
            .ValidateOnStart();
        services
            .AddOptions<FastingNotificationOptions>()
            .BindConfiguration(FastingNotificationOptions.SectionName)
            .Validate(FastingNotificationOptions.HasValidConfiguration,
                "FastingNotifications:PollIntervalSeconds must be greater than zero when enabled.")
            .ValidateOnStart();
        services
            .AddOptions<ApiBuildInfoOptions>()
            .BindConfiguration(ApiBuildInfoOptions.SectionName)
            .Validate(ApiBuildInfoOptions.HasValidCommitSha,
                "BuildInfo:CommitSha must be empty or shorter than 129 characters.")
            .Validate(ApiBuildInfoOptions.HasValidImageTag,
                "BuildInfo:ImageTag must be empty or shorter than 257 characters.")
            .ValidateOnStart();
        services.AddSingleton<IConfigureOptions<Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions>, CorsOptionsSetup>();
        services.AddSingleton<IConfigureOptions<ForwardedHeadersOptions>, ForwardedHeadersOptionsSetup>();
        services.AddSingleton<IConfigureOptions<Microsoft.AspNetCore.RateLimiting.RateLimiterOptions>, RateLimiterOptionsSetup>();
        services.AddSingleton<IConfigureOptions<Microsoft.AspNetCore.OutputCaching.OutputCacheOptions>, OutputCacheOptionsSetup>();
        services.AddSingleton(static serviceProvider => {
            var options = serviceProvider.GetRequiredService<IOptions<ApiBuildInfoOptions>>().Value;
            var environment = serviceProvider.GetRequiredService<IHostEnvironment>();
            return ApiBuildInfo.Create(options, environment.EnvironmentName);
        });
        services.AddCors(static _ => { });

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((options, jwtOptionsAccessor) => {
                var jwtOptions = jwtOptionsAccessor.Value;
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
                            (path.StartsWithSegments("/hubs/email-verification") ||
                                path.StartsWithSegments("/hubs/notifications"))) {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    },
                };
            });

        services.AddAuthorization();
        services.AddHttpLogging(options => {
            options.LoggingFields = HttpLoggingFields.RequestMethod |
                                    HttpLoggingFields.RequestPath |
                                    HttpLoggingFields.ResponseStatusCode |
                                    HttpLoggingFields.Duration;
            options.RequestHeaders.Add("X-Correlation-Id");
            options.MediaTypeOptions.AddText("application/json");
        });
        services.AddProblemDetails();
        services.AddExceptionHandler<ApiExceptionHandler>();
        services.AddRateLimiter(static _ => { });
        services.AddOutputCache(static _ => { });
        services.AddHostedService<FastingNotificationHostedService>();
        services.AddPresentationApi();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options => {
            options.SwaggerDoc("v1", new OpenApiInfo {
                Title = "FoodDiary API",
                Version = "v1",
            });
            options.OperationFilter<StandardErrorResponsesOperationFilter>();
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
        services.AddConfiguredOpenTelemetry();
        services.AddApiHealthChecks();

        return services;
    }

    private static IServiceCollection AddApiHealthChecks(this IServiceCollection services) {
        services
            .AddHealthChecks()
            .AddDbContextCheck<FoodDiaryDbContext>("postgresql", tags: ["ready"])
            .AddCheck<S3HealthCheck>("s3", tags: ["ready"]);

        return services;
    }

    private static IServiceCollection AddConfiguredOpenTelemetry(this IServiceCollection services) {
        services.AddSingleton<TracerProvider>(static serviceProvider => {
            var options = serviceProvider.GetRequiredService<IOptions<OpenTelemetryOptions>>().Value;
            if (string.IsNullOrWhiteSpace(options.Otlp.Endpoint)) {
                return null!;
            }

            var endpointUri = new Uri(options.Otlp.Endpoint, UriKind.Absolute);

            return Sdk.CreateTracerProviderBuilder()
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("FoodDiary.Web.Api"))
                .AddSource(ApiTelemetry.TelemetryName)
                .AddSource(PresentationApiTelemetry.TelemetryName)
                .AddOtlpExporter(exporterOptions => exporterOptions.Endpoint = endpointUri)
                .Build();
        });
        services.AddSingleton<MeterProvider>(static serviceProvider => {
            var options = serviceProvider.GetRequiredService<IOptions<OpenTelemetryOptions>>().Value;
            if (string.IsNullOrWhiteSpace(options.Otlp.Endpoint)) {
                return null!;
            }

            var endpointUri = new Uri(options.Otlp.Endpoint, UriKind.Absolute);

            return Sdk.CreateMeterProviderBuilder()
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("FoodDiary.Web.Api"))
                .AddMeter(ApiTelemetry.TelemetryName)
                .AddMeter(PresentationApiTelemetry.TelemetryName)
                .AddMeter("FoodDiary.Application.Ai")
                .AddMeter("FoodDiary.Application.Email")
                .AddMeter("FoodDiary.Infrastructure")
                .AddMeter("FoodDiary.Integrations")
                .AddOtlpExporter(exporterOptions => exporterOptions.Endpoint = endpointUri)
                .Build();
        });

        return services;
    }
}
