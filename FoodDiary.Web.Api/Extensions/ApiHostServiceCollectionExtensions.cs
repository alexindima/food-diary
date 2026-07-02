using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Web.Api.HealthChecks;
using FoodDiary.Web.Api.Options;
using FoodDiary.Web.Api.Services;
using FoodDiary.Web.Api.Swagger;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.OpenApi;

namespace FoodDiary.Web.Api.Extensions;

public static class ApiHostServiceCollectionExtensions {
    extension(IServiceCollection services) {
        internal IServiceCollection AddApiHostServices() {
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
            services.AddHostedService<InitialAdminHostedService>();
            services.AddHostedService<UserLoginEventCleanupHostedService>();
            services.AddHostedService<ImageObjectDeletionOutboxHostedService>();
            services.AddHostedService<NotificationWebPushOutboxHostedService>();

            return services;
        }

        internal IServiceCollection AddApiDataProtection(IConfiguration configuration) {
            ApiDataProtectionOptions options = configuration
                .GetSection(ApiDataProtectionOptions.SectionName)
                .Get<ApiDataProtectionOptions>() ?? new ApiDataProtectionOptions();

            IDataProtectionBuilder builder = services
                .AddDataProtection()
                .SetApplicationName(options.ApplicationName);

            if (!string.IsNullOrWhiteSpace(options.KeyRingPath)) {
                builder.PersistKeysToFileSystem(new DirectoryInfo(options.KeyRingPath));
            }

            return services;
        }

        internal IServiceCollection AddApiSwagger() {
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
                    [new OpenApiSecuritySchemeReference("Bearer", document, externalResource: null)] = [],
                });
            });

            return services;
        }

        internal IServiceCollection AddApiHealthChecks() {
            services
                .AddHealthChecks()
                .AddDbContextCheck<FoodDiaryDbContext>("postgresql", tags: ["ready"])
                .AddCheck<DistributedCacheHealthCheck>("distributed-cache", tags: ["ready"])
                .AddCheck<S3HealthCheck>("s3", tags: ["ready"]);

            return services;
        }
    }
}
