using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FoodDiary.Application.Authentication.Abstractions;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Common.Abstractions.Audit;
using FoodDiary.Application.Ai.Common;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Cycles.Common;
using FoodDiary.Application.DailyAdvices.Common;
using FoodDiary.Application.Hydration.Common;
using FoodDiary.Application.Images.Common;
using FoodDiary.Application.Meals.Common;
using FoodDiary.Application.Products.Common;
using FoodDiary.Application.RecentItems.Common;
using FoodDiary.Application.Recipes.Common;
using FoodDiary.Application.ShoppingLists.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.WaistEntries.Common;
using FoodDiary.Application.WeightEntries.Common;
using FoodDiary.Infrastructure.Authentication;
using FoodDiary.Infrastructure.Persistence.Admin;
using FoodDiary.Infrastructure.Persistence.Ai;
using FoodDiary.Infrastructure.Persistence.Images;
using FoodDiary.Infrastructure.Persistence.Meals;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Interceptors;
using FoodDiary.Infrastructure.Persistence.Products;
using FoodDiary.Infrastructure.Persistence.RecentItems;
using FoodDiary.Infrastructure.Persistence.Recipes;
using FoodDiary.Infrastructure.Persistence.ShoppingLists;
using FoodDiary.Infrastructure.Persistence.Tracking;
using FoodDiary.Infrastructure.Persistence.Users;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using FoodDiary.Infrastructure.Options;
using FoodDiary.Infrastructure.Events;
using FoodDiary.Infrastructure.Services;
using FoodDiary.Application.Common.Abstractions.Events;
using FoodDiary.Application.Common.Abstractions.Persistence;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;

namespace FoodDiary.Infrastructure;

public static class DependencyInjection {
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration) {
        services.AddMemoryCache();
        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .Validate(static options => !options.EnableRetries || options.MaxRetryCount > 0,
                "Database:MaxRetryCount must be greater than zero when retries are enabled.")
            .Validate(static options => !options.EnableRetries || options.MaxRetryDelaySeconds > 0,
                "Database:MaxRetryDelaySeconds must be greater than zero when retries are enabled.")
            .ValidateOnStart();
        services.AddSingleton<DatabaseCommandTelemetryInterceptor>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IDomainEventPublisher, MediatRDomainEventPublisher>();
        services.AddScoped<DomainEventDispatchInterceptor>();
        services.AddDbContext<FoodDiaryDbContext>((sp, options) => {
            var databaseOptions = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            options
                .UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"),
                    npgsqlOptions => {
                        if (databaseOptions.EnableRetries) {
                            npgsqlOptions.EnableRetryOnFailure(
                                databaseOptions.MaxRetryCount,
                                TimeSpan.FromSeconds(databaseOptions.MaxRetryDelaySeconds),
                                errorCodesToAdd: null);
                        }
                    })
                .AddInterceptors(
                    sp.GetRequiredService<DatabaseCommandTelemetryInterceptor>(),
                    sp.GetRequiredService<DomainEventDispatchInterceptor>());
        });

        services.AddOptions<S3Options>()
            .Bind(configuration.GetSection(S3Options.SectionName))
            .Validate(static options => options.MaxUploadSizeBytes > 0,
                "S3:MaxUploadSizeBytes must be greater than zero.")
            .Validate(static options => string.IsNullOrWhiteSpace(options.PublicBaseUrl) || Uri.IsWellFormedUriString(options.PublicBaseUrl, UriKind.Absolute),
                "S3:PublicBaseUrl must be an absolute URL when provided.")
            .Validate(static options => string.IsNullOrWhiteSpace(options.ServiceUrl) || Uri.IsWellFormedUriString(options.ServiceUrl, UriKind.Absolute),
                "S3:ServiceUrl must be an absolute URL when provided.")
            .ValidateOnStart();
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(static options => !string.IsNullOrWhiteSpace(options.SecretKey) && options.SecretKey.Length >= 32,
                $"{JwtOptions.SectionName}:SecretKey must be at least 32 characters long.")
            .Validate(static options => !string.IsNullOrWhiteSpace(options.Issuer),
                $"{JwtOptions.SectionName}:Issuer is required.")
            .Validate(static options => !string.IsNullOrWhiteSpace(options.Audience),
                $"{JwtOptions.SectionName}:Audience is required.")
            .Validate(static options => options.ExpirationMinutes > 0,
                $"{JwtOptions.SectionName}:ExpirationMinutes must be greater than zero.")
            .Validate(static options => options.RefreshTokenExpirationDays > 0,
                $"{JwtOptions.SectionName}:RefreshTokenExpirationDays must be greater than zero.")
            .ValidateOnStart();
        services.AddOptions<TelegramAuthOptions>()
            .Bind(configuration.GetSection(TelegramAuthOptions.SectionName));
        services.AddOptions<OpenAiOptions>()
            .Bind(configuration.GetSection(OpenAiOptions.SectionName))
            .Validate(static options => string.IsNullOrWhiteSpace(options.VisionModel) || !string.IsNullOrWhiteSpace(options.VisionFallbackModel),
                "OpenAi:VisionFallbackModel is required when VisionModel is configured.")
            .Validate(static options => string.IsNullOrWhiteSpace(options.ApiKey) || !string.IsNullOrWhiteSpace(options.TextModel),
                "OpenAi:TextModel is required when ApiKey is configured.")
            .Validate(static options => string.IsNullOrWhiteSpace(options.ApiKey) || !string.IsNullOrWhiteSpace(options.VisionModel),
                "OpenAi:VisionModel is required when ApiKey is configured.")
            .ValidateOnStart();
        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName))
            .Validate(static options => options.SmtpPort > 0,
                "Email:SmtpPort must be greater than zero.")
            .Validate(static options => string.IsNullOrWhiteSpace(options.FrontendBaseUrl) || Uri.IsWellFormedUriString(options.FrontendBaseUrl, UriKind.Absolute),
                "Email:FrontendBaseUrl must be an absolute URL when provided.")
            .Validate(static options => !string.IsNullOrWhiteSpace(options.VerificationPath),
                "Email:VerificationPath is required.")
            .Validate(static options => !string.IsNullOrWhiteSpace(options.PasswordResetPath),
                "Email:PasswordResetPath is required.")
            .ValidateOnStart();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ProductRepository>();
        services.AddScoped<IProductRepository, CachedProductRepository>();
        services.AddScoped<IProductLookupService, ProductLookupService>();
        services.AddScoped<IRecipeRepository, RecipeRepository>();
        services.AddScoped<IRecipeLookupService, RecipeLookupService>();
        services.AddScoped<IRecentItemRepository, RecentItemRepository>();
        services.AddScoped<IMealRepository, MealRepository>();
        services.AddScoped<IShoppingListRepository, ShoppingListRepository>();
        services.AddScoped<IWeightEntryRepository, WeightEntryRepository>();
        services.AddScoped<IWaistEntryRepository, WaistEntryRepository>();
        services.AddScoped<IHydrationEntryRepository, HydrationEntryRepository>();
        services.AddScoped<IDailyAdviceRepository, DailyAdviceRepository>();
        services.AddScoped<ICycleRepository, CycleRepository>();
        services.AddScoped<IImageAssetRepository, ImageAssetRepository>();
        services.AddScoped<IAiUsageRepository, AiUsageRepository>();
        services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();
        services.AddSingleton<IAmazonS3>(sp => {
            var s3Options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<S3Options>>().Value;
            var credentials = new BasicAWSCredentials(s3Options.AccessKeyId, s3Options.SecretAccessKey);
            var regionValue = s3Options.Region?.Trim();
            var regionEndpoint = !string.IsNullOrWhiteSpace(regionValue)
                ? RegionEndpoint.GetBySystemName(regionValue)
                : RegionEndpoint.USEast1;
            var config = new AmazonS3Config {
                RegionEndpoint = regionEndpoint,
                AuthenticationRegion = regionEndpoint.SystemName,
                ServiceURL = string.IsNullOrWhiteSpace(s3Options.ServiceUrl) ? null : s3Options.ServiceUrl,
                ForcePathStyle = !string.IsNullOrWhiteSpace(s3Options.ServiceUrl)
            };
            return new AmazonS3Client(credentials, config);
        });
        services.AddSingleton<IObjectStorageClient, S3ObjectStorageClient>();
        services.AddSingleton<IImageStorageService, S3ImageStorageService>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITelegramAuthValidator, TelegramAuthValidator>();
        services.AddSingleton<ITelegramLoginWidgetValidator, TelegramLoginWidgetValidator>();
        services.AddSingleton<IAdminSsoService, AdminSsoService>();
        services.AddScoped<IUserCleanupService, UserCleanupService>();
        services.AddSingleton<IEmailTemplateProvider, EmailTemplateProvider>();
        services.AddSingleton<IEmailTransport, SmtpClientEmailTransport>();
        services.AddSingleton<IEmailSender, SmtpEmailSender>();
        services.AddSingleton<IAuditLogger, StructuredAuditLogger>();
        services.AddHttpClient<IOpenAiFoodService, OpenAiFoodService>(client => {
            client.Timeout = TimeSpan.FromSeconds(60);
        })
        .AddResilienceHandler("openai-circuit-breaker", builder => {
            builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 3,
                BreakDuration = TimeSpan.FromSeconds(30),
            });
        });

        return services;
    }
}
