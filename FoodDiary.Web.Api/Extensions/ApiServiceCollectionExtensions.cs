using System.Text;
using FoodDiary.Application;
using FoodDiary.Infrastructure;
using FoodDiary.Infrastructure.Options;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Options;
using FoodDiary.Web.Api.Options;
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
        services.AddDistributedMemoryCache();
        services
            .AddOptions<ApiCorsOptions>()
            .BindConfiguration(ApiCorsOptions.SectionName)
            .Validate(ApiCorsOptions.HasValidOrigins,
                "Cors:Origins must contain at least one absolute origin URL.")
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
        services.AddSingleton<IConfigureOptions<Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions>, CorsOptionsSetup>();
        services.AddSingleton<IConfigureOptions<Microsoft.AspNetCore.RateLimiting.RateLimiterOptions>, RateLimiterOptionsSetup>();
        services.AddSingleton<IConfigureOptions<Microsoft.AspNetCore.OutputCaching.OutputCacheOptions>, OutputCacheOptionsSetup>();
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
                            path.StartsWithSegments("/hubs/email-verification")) {
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
            options.RequestHeaders.Add("X-Forwarded-For");
            options.RequestHeaders.Add("X-Correlation-Id");
            options.MediaTypeOptions.AddText("application/json");
        });
        services.AddProblemDetails();
        services.AddExceptionHandler<ApiExceptionHandler>();
        services.AddRateLimiter(static _ => { });
        services.AddOutputCache(static _ => { });
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
        services.AddConfiguredOpenTelemetry(configuration);

        return services;
    }

    private static IServiceCollection AddConfiguredOpenTelemetry(this IServiceCollection services, IConfiguration configuration) {
        var openTelemetryOptions = new OpenTelemetryOptions();
        configuration.GetSection(OpenTelemetryOptions.SectionName).Bind(openTelemetryOptions);

        if (string.IsNullOrWhiteSpace(openTelemetryOptions.Otlp.Endpoint)) {
            return services;
        }

        if (!OpenTelemetryOptions.HasValidOtlpEndpoint(openTelemetryOptions)) {
            throw new InvalidOperationException("OpenTelemetry:Otlp:Endpoint must be a valid absolute URI.");
        }

        var endpointUri = new Uri(openTelemetryOptions.Otlp.Endpoint, UriKind.Absolute);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("FoodDiary.Web.Api"))
            .WithTracing(tracing => tracing
                .AddSource(ApiTelemetry.TelemetryName)
                .AddSource(PresentationApiTelemetry.TelemetryName)
                .AddOtlpExporter(options => options.Endpoint = endpointUri))
            .WithMetrics(metrics => metrics
                .AddMeter(ApiTelemetry.TelemetryName)
                .AddMeter(PresentationApiTelemetry.TelemetryName)
                .AddOtlpExporter(options => options.Endpoint = endpointUri));

        return services;
    }
}
