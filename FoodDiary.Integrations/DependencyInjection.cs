using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Abstractions.Email.Common;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Abstractions.OpenFoodFacts.Common;
using FoodDiary.Application.Abstractions.Usda.Common;
using FoodDiary.Application.Abstractions.Wearables.Common;
using FoodDiary.Integrations.Authentication;
using FoodDiary.Integrations.Billing;
using FoodDiary.Integrations.Options;
using FoodDiary.Integrations.Services;
using FoodDiary.Integrations.Services.MailInbox;
using FoodDiary.Integrations.Services.OpenAi;
using FoodDiary.Integrations.Wearables;
using FoodDiary.MailInbox.Client.Extensions;
using FoodDiary.MailInbox.Client.Options;
using FoodDiary.MailRelay.Client.Extensions;
using FoodDiary.MailRelay.Client.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;

namespace FoodDiary.Integrations;

public static class DependencyInjection {
    public static IServiceCollection AddIntegrations(this IServiceCollection services, IConfiguration configuration) {
        services.AddOptions<S3Options>()
            .Bind(configuration.GetSection(S3Options.SectionName))
            .Validate(S3Options.HasValidMaxUploadSize,
                "S3:MaxUploadSizeBytes must be greater than zero.")
            .Validate(S3Options.HasValidPublicBaseUrl,
                "S3:PublicBaseUrl must be an absolute URL when provided.")
            .Validate(S3Options.HasValidServiceUrl,
                "S3:ServiceUrl must be an absolute URL when provided.")
            .ValidateOnStart();
        services.AddOptions<OpenAiOptions>()
            .Bind(configuration.GetSection(OpenAiOptions.SectionName))
            .Validate(OpenAiOptions.HasVisionFallbackWhenVisionModelConfigured,
                "OpenAi:VisionFallbackModel is required when VisionModel is configured.")
            .Validate(OpenAiOptions.HasTextModelWhenApiKeyConfigured,
                "OpenAi:TextModel is required when ApiKey is configured.")
            .Validate(OpenAiOptions.HasVisionModelWhenApiKeyConfigured,
                "OpenAi:VisionModel is required when ApiKey is configured.")
            .ValidateOnStart();
        services.AddOptions<GoogleAuthOptions>()
            .Bind(configuration.GetSection(GoogleAuthOptions.SectionName))
            .Validate(GoogleAuthOptions.HasValidClientId,
                "GoogleAuth:ClientId must be empty or a non-whitespace value.")
            .ValidateOnStart();
        services.AddOptions<TelegramAuthOptions>()
            .Bind(configuration.GetSection(TelegramAuthOptions.SectionName));
        services.AddOptions<BillingOptions>()
            .Bind(configuration.GetSection(BillingOptions.SectionName))
            .Validate(static options => !string.IsNullOrWhiteSpace(options.Provider),
                "Billing:Provider is required.")
            .Validate(static options => Domain.Entities.Billing.BillingProviderNames.IsSupported(options.Provider),
                "Billing:Provider must be a supported billing provider.")
            .ValidateOnStart();
        services.AddOptions<StripeOptions>()
            .Bind(configuration.GetSection(StripeOptions.SectionName))
            .Validate(static options =>
                    string.IsNullOrWhiteSpace(options.SecretKey) ||
                    (!string.IsNullOrWhiteSpace(options.WebhookSecret) &&
                     !string.IsNullOrWhiteSpace(options.PremiumMonthlyPriceId) &&
                     !string.IsNullOrWhiteSpace(options.PremiumYearlyPriceId) &&
                     Uri.IsWellFormedUriString(options.SuccessUrl, UriKind.Absolute) &&
                     Uri.IsWellFormedUriString(options.CancelUrl, UriKind.Absolute) &&
                     Uri.IsWellFormedUriString(options.PortalReturnUrl, UriKind.Absolute)),
                "Stripe configuration is incomplete.");
        services.AddOptions<PaddleOptions>()
            .Bind(configuration.GetSection(PaddleOptions.SectionName));
        services.AddOptions<WebPushOptions>()
            .Bind(configuration.GetSection(WebPushOptions.SectionName))
            .Validate(WebPushOptions.HasValidConfiguration,
                "WebPush configuration is invalid.")
            .ValidateOnStart();

        services.AddSingleton<IAmazonS3>(sp => {
            var s3Options = sp.GetRequiredService<IOptions<S3Options>>().Value;
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

        services.AddMailRelayClient(options => {
            var section = configuration.GetSection(MailRelayClientOptions.SectionName);
            options.BaseUrl = section["BaseUrl"] ?? string.Empty;
            options.ApiKey = section["ApiKey"] ?? string.Empty;
            options.Timeout = TimeSpan.FromSeconds(15);
        });
        services.AddMailInboxClient(options => {
            var section = configuration.GetSection(MailInboxClientOptions.SectionName);
            options.BaseUrl = section["BaseUrl"] ?? string.Empty;
            options.Timeout = TimeSpan.FromSeconds(15);
        });
        services.AddScoped<IAdminMailInboxReader, MailInboxClientAdminMailInboxReader>();
        services.AddSingleton<RelayEmailTransport>();
        services.AddSingleton<IEmailTransport>(static sp => sp.GetRequiredService<RelayEmailTransport>());
        services.AddSingleton<IGoogleTokenValidator, GoogleTokenValidator>();
        services.AddSingleton<ITelegramAuthValidator, TelegramAuthValidator>();
        services.AddSingleton<ITelegramLoginWidgetValidator, TelegramLoginWidgetValidator>();
        services.AddSingleton<IBillingPublicConfigProvider, BillingPublicConfigProvider>();
        services.AddScoped<IBillingProviderGateway, StripeBillingGateway>();
        services.AddHttpClient<PaddleBillingGateway>(client => {
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddScoped<IBillingProviderGateway>(sp => sp.GetRequiredService<PaddleBillingGateway>());
        services.AddScoped<IBillingProviderGatewayAccessor, ConfigurableBillingProviderGatewayAccessor>();
        services.AddScoped<IWebPushNotificationSender, WebPushNotificationSender>();
        services.AddScoped<IWebPushConfigurationProvider, WebPushNotificationSender>();
        services.AddHttpClient<IOpenAiFoodClient, OpenAiFoodClient>(client => {
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

        services.Configure<UsdaApiOptions>(configuration.GetSection(UsdaApiOptions.SectionName));
        services.AddHttpClient<IUsdaFoodSearchService, UsdaFoodSearchService>(client => {
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        services.Configure<OpenFoodFactsApiOptions>(configuration.GetSection(OpenFoodFactsApiOptions.SectionName));
        services.AddHttpClient<IOpenFoodFactsService, OpenFoodFactsService>(client => {
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.Configure<FitbitOptions>(configuration.GetSection(FitbitOptions.SectionName));
        services.Configure<GoogleFitOptions>(configuration.GetSection(GoogleFitOptions.SectionName));
        services.AddHttpClient<FitbitClient>(client => { client.Timeout = TimeSpan.FromSeconds(30); });
        services.AddHttpClient<GoogleFitClient>(client => { client.Timeout = TimeSpan.FromSeconds(30); });
        services.AddScoped<IWearableClient>(sp => sp.GetRequiredService<FitbitClient>());
        services.AddScoped<IWearableClient>(sp => sp.GetRequiredService<GoogleFitClient>());

        return services;
    }
}
