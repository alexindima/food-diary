using Amazon.S3;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Abstractions.Email.Common;
using FoodDiary.Application.Abstractions.Export.Common;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Abstractions.OpenFoodFacts.Common;
using FoodDiary.Application.Abstractions.Usda.Common;
using FoodDiary.Application.Abstractions.Wearables.Common;
using FoodDiary.Application.Abstractions.Dashboard.Common;
using FoodDiary.Application.Abstractions.Cycles.Common;
using FoodDiary.Application.Abstractions.DailyAdvices.Common;
using FoodDiary.Application.Abstractions.Exercises.Common;
using FoodDiary.Application.Abstractions.Hydration.Common;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.WaistEntries.Common;
using FoodDiary.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Options;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Dashboard;
using FoodDiary.Infrastructure.Persistence.Dietologist;
using FoodDiary.Infrastructure.Persistence.Tracking;
using FoodDiary.Infrastructure.Services;
using FoodDiary.Modules.Fasting.Infrastructure;
using FoodDiary.Integrations;
using FoodDiary.Integrations.Billing;
using FoodDiary.Integrations.Options;
using FoodDiary.Integrations.Services;
using FoodDiary.Integrations.Services.MailInbox;
using FoodDiary.MailInbox.Client;
using FoodDiary.Integrations.Services.OpenAi;
using FoodDiary.Integrations.Wearables;
using FoodDiary.MailRelay.Client.Options;
using FoodDiary.Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Metadata;
using WebPush;

namespace FoodDiary.Infrastructure.Tests;

[ExcludeFromCodeCoverage]
public sealed class DependencyInjectionTests {
    [Fact]
    public void AddInfrastructure_WithInvalidEmailBaseUrl_FailsOptionsValidation() {
        var services = new ServiceCollection();
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=food_diary;Username=test;Password=test",
            ["Jwt:SecretKey"] = "super-secret-key-for-tests-only-123456789",
            ["Jwt:Issuer"] = "FoodDiary",
            ["Jwt:Audience"] = "FoodDiaryClients",
            ["Jwt:ExpirationMinutes"] = "60",
            ["Jwt:RefreshTokenExpirationDays"] = "7",
            ["Jwt:RememberMeRefreshTokenExpirationDays"] = "90",
            ["Email:FrontendBaseUrl"] = "not-a-url",
        });

        services.AddInfrastructure(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        OptionsValidationException ex = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<EmailOptions>>().Value);
        Assert.Contains("FrontendBaseUrl", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddInfrastructure_WithInvalidAllowedEmailBaseUrl_FailsOptionsValidation() {
        var services = new ServiceCollection();
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=food_diary;Username=test;Password=test",
            ["Jwt:SecretKey"] = "super-secret-key-for-tests-only-123456789",
            ["Jwt:Issuer"] = "FoodDiary",
            ["Jwt:Audience"] = "FoodDiaryClients",
            ["Jwt:ExpirationMinutes"] = "60",
            ["Jwt:RefreshTokenExpirationDays"] = "7",
            ["Jwt:RememberMeRefreshTokenExpirationDays"] = "90",
            ["Email:FrontendBaseUrl"] = "https://fooddiary.club",
            ["Email:AllowedFrontendBaseUrls:0"] = "not-a-url",
        });

        services.AddInfrastructure(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        OptionsValidationException ex = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<EmailOptions>>().Value);
        Assert.Contains("AllowedFrontendBaseUrls", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddIntegrations_WithInvalidMailRelayClientBaseUrl_FailsOptionsValidation() {
        var services = new ServiceCollection();
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=food_diary;Username=test;Password=test",
            ["Jwt:SecretKey"] = "super-secret-key-for-tests-only-123456789",
            ["Jwt:Issuer"] = "FoodDiary",
            ["Jwt:Audience"] = "FoodDiaryClients",
            ["Jwt:ExpirationMinutes"] = "60",
            ["Jwt:RefreshTokenExpirationDays"] = "7",
            ["Jwt:RememberMeRefreshTokenExpirationDays"] = "90",
            ["MailRelayClient:BaseUrl"] = "not-a-url",
        });

        services.AddIntegrations(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        OptionsValidationException ex = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<MailRelayClientOptions>>().Value);
        Assert.Contains("base URL", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddIntegrations_WithInvalidS3ServiceUrl_FailsOptionsValidation() {
        var services = new ServiceCollection();
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=food_diary;Username=test;Password=test",
            ["Jwt:SecretKey"] = "super-secret-key-for-tests-only-123456789",
            ["Jwt:Issuer"] = "FoodDiary",
            ["Jwt:Audience"] = "FoodDiaryClients",
            ["Jwt:ExpirationMinutes"] = "60",
            ["Jwt:RefreshTokenExpirationDays"] = "7",
            ["Jwt:RememberMeRefreshTokenExpirationDays"] = "90",
            ["S3:ServiceUrl"] = "invalid-url",
        });

        services.AddIntegrations(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        OptionsValidationException ex = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<FoodDiary.Integrations.Options.S3Options>>().Value);
        Assert.Contains("ServiceUrl", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddIntegrations_WithOptionalProvidersNotConfigured_RegistersSafeStorageFallbackOnly() {
        var services = new ServiceCollection();
        services.AddLogging();
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>(StringComparer.Ordinal));

        services.AddIntegrations(configuration);
        await using ServiceProvider provider = services.BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        IImageStorageService imageStorage = provider.GetRequiredService<IImageStorageService>();
        Assert.IsType<UnconfiguredImageStorageService>(imageStorage);
        Assert.Empty(scope.ServiceProvider.GetServices<IWearableClient>());

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            imageStorage.CreatePresignedUploadAsync(
                UserId.New(),
                "meal.webp",
                "image/webp",
                1024,
                CancellationToken.None));
        ImageObjectValidationResult validation = await imageStorage.ValidateUploadedObjectAsync(
            "users/test/meal.webp",
            CancellationToken.None);

        Assert.Contains("not configured", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(validation.IsValid);
        Assert.Equal("storage_not_configured", validation.ErrorCode);
    }

    [Fact]
    public void AddIntegrations_WithNonPositiveTelegramAuthTtl_FailsOptionsValidation() {
        var services = new ServiceCollection();
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["TelegramAuth:BotToken"] = "test-token",
            ["TelegramAuth:AuthTtlSeconds"] = "0",
        });

        services.AddIntegrations(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        OptionsValidationException ex = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<TelegramAuthOptions>>().Value);
        Assert.Contains("AuthTtlSeconds", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddIntegrations_RegistersIntegrationServicesAndTypedHttpClients() {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(TimeProvider.System);
        IConfiguration configuration = CreateValidIntegrationsConfiguration();

        services.AddIntegrations(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        Assert.IsType<AmazonS3Client>(provider.GetRequiredService<IAmazonS3>());
        Assert.IsType<S3ObjectStorageClient>(provider.GetRequiredService<IObjectStorageClient>());
        Assert.IsType<S3ImageStorageService>(provider.GetRequiredService<IImageStorageService>());
        Assert.IsType<RelayEmailTransport>(scope.ServiceProvider.GetRequiredService<RelayEmailTransport>());
        Assert.IsType<RelayEmailTransport>(scope.ServiceProvider.GetRequiredService<IEmailTransport>());
        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IEmailTransport) &&
                          descriptor.Lifetime == ServiceLifetime.Scoped);
        Assert.NotNull(provider.GetRequiredService<IGoogleTokenValidator>());
        Assert.NotNull(provider.GetRequiredService<ITelegramAuthValidator>());
        Assert.NotNull(provider.GetRequiredService<ITelegramLoginWidgetValidator>());
        Assert.IsType<BillingPublicConfigProvider>(provider.GetRequiredService<IBillingPublicConfigProvider>());
        Assert.IsType<ConfigurableBillingProviderGatewayAccessor>(scope.ServiceProvider.GetRequiredService<IBillingProviderGatewayAccessor>());
        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IWebPushNotificationSender) &&
                          descriptor.ImplementationType == typeof(WebPushNotificationSender) &&
                          descriptor.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IWebPushConfigurationProvider) &&
                          descriptor.ImplementationType == typeof(WebPushNotificationSender) &&
                          descriptor.Lifetime == ServiceLifetime.Scoped);
        Assert.IsType<OpenAiFoodClient>(scope.ServiceProvider.GetRequiredService<IOpenAiFoodClient>());
        Assert.IsType<UsdaFoodSearchService>(scope.ServiceProvider.GetRequiredService<IUsdaFoodSearchService>());
        Assert.IsType<OpenFoodFactsService>(scope.ServiceProvider.GetRequiredService<IOpenFoodFactsService>());
        Assert.IsType<MailInboxClientAdminMailInboxReader>(scope.ServiceProvider.GetRequiredService<IAdminMailInboxReader>());

        IReadOnlyList<IBillingProviderGateway> gateways = scope.ServiceProvider.GetServices<IBillingProviderGateway>().ToList();
        Assert.Contains(gateways, gateway => gateway is StripeBillingGateway);
        Assert.Contains(gateways, gateway => gateway is PaddleBillingGateway);
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<PaddleNotificationRecoveryService>());
        Assert.Contains(gateways, gateway => gateway is YooKassaBillingGateway);
        Assert.IsType<YooKassaBillingGateway>(scope.ServiceProvider.GetRequiredService<IBillingRecurringProviderGateway>());

        IReadOnlyList<IWearableClient> wearableClients = scope.ServiceProvider.GetServices<IWearableClient>().ToList();
        IWearableClient wearableClient = Assert.Single(wearableClients);
        Assert.Equal(WearableProvider.Fitbit, wearableClient.Provider);
    }

    [Fact]
    public void AddIntegrations_WithoutMailInboxConfiguration_DoesNotRegisterMailInboxAccess() {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(TimeProvider.System);
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(CreateValidIntegrationsConfiguration()
                .AsEnumerable()
                .Where(static pair => !pair.Key.StartsWith("MailInboxClient:", StringComparison.OrdinalIgnoreCase)))
            .Build();

        services.AddIntegrations(configuration);

        Assert.DoesNotContain(services, static descriptor => descriptor.ServiceType == typeof(IMailInboxClient));
        Assert.DoesNotContain(services, static descriptor => descriptor.ServiceType == typeof(IAdminMailInboxReader));
    }

    [Fact]
    public void AddIntegrations_WithOnlyLegacyMailInboxApiKey_DoesNotRegisterMailInboxAccess() {
        var services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase) {
                ["MailInboxClient:ApiKey"] = "legacy-key",
            })
            .Build();

        services.AddIntegrations(configuration);

        Assert.DoesNotContain(services, static descriptor => descriptor.ServiceType == typeof(IMailInboxClient));
        Assert.DoesNotContain(services, static descriptor => descriptor.ServiceType == typeof(IAdminMailInboxReader));
    }

    [Fact]
    public void AddIntegrations_WithIncompleteCurrentMailInboxConfiguration_PreservesOptionsValidation() {
        var services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase) {
                ["MailInboxClient:MetadataApiKey"] = "metadata-key",
            })
            .Build();

        services.AddIntegrations(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<FoodDiary.MailInbox.Client.Options.MailInboxClientOptions>>().Value);
    }

    [Fact]
    public void AddIntegrations_ConfiguresNamedHttpClientTimeoutsAndHeaders() {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(TimeProvider.System);
        IConfiguration configuration = CreateValidIntegrationsConfiguration();

        services.AddIntegrations(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();
        IHttpClientFactory factory = provider.GetRequiredService<IHttpClientFactory>();

        Assert.Equal(TimeSpan.FromSeconds(30), factory.CreateClient(nameof(PaddleBillingGateway)).Timeout);
        Assert.Equal(TimeSpan.FromSeconds(30), factory.CreateClient(nameof(YooKassaBillingGateway)).Timeout);
        Assert.Equal(TimeSpan.FromSeconds(15), factory.CreateClient(nameof(IUsdaFoodSearchService)).Timeout);
        HttpClient openFoodFactsClient = factory.CreateClient(nameof(IOpenFoodFactsService));
        Assert.Equal(TimeSpan.FromSeconds(10), openFoodFactsClient.Timeout);
        Assert.Contains(openFoodFactsClient.DefaultRequestHeaders.UserAgent, value => string.Equals(value.Product?.Name, "FoodDiaryTests", StringComparison.Ordinal));
        Assert.Equal(TimeSpan.FromSeconds(30), factory.CreateClient(nameof(IWebPushClientAdapter)).Timeout);
        Assert.Equal(TimeSpan.FromSeconds(30), factory.CreateClient(nameof(FitbitClient)).Timeout);
    }

    [Theory]
    [InlineData(nameof(IUsdaFoodSearchService))]
    [InlineData(nameof(IOpenFoodFactsService))]
    [InlineData(nameof(PaddleBillingGateway))]
    [InlineData(nameof(PaddleNotificationRecoveryService))]
    [InlineData(nameof(YooKassaBillingGateway))]
    [InlineData(nameof(IWebPushClientAdapter))]
    public async Task AddIntegrations_SensitiveQueryClients_DoNotUseDefaultHttpClientLogging(string clientName) {
        var services = new ServiceCollection();
        var loggerProvider = new RecordingLoggerProvider();
        services.AddLogging(builder => builder
            .SetMinimumLevel(LogLevel.Trace)
            .AddProvider(loggerProvider));
        services.AddSingleton(TimeProvider.System);
        services.AddIntegrations(CreateValidIntegrationsConfiguration());
        services.AddHttpClient(clientName)
            .ConfigurePrimaryHttpMessageHandler(static () => new SuccessfulHttpMessageHandler());
        await using ServiceProvider provider = services.BuildServiceProvider();
        IHttpClientFactory factory = provider.GetRequiredService<IHttpClientFactory>();
        string sensitiveValue = $"sensitive-{Guid.NewGuid():N}";

        using HttpResponseMessage response = await factory
            .CreateClient(clientName)
            .GetAsync($"https://example.test/resource?value={sensitiveValue}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain(
            loggerProvider.Messages,
            message => message.Contains(sensitiveValue, StringComparison.Ordinal));
    }

    [Fact]
    public void AddIntegrations_WithInvalidBillingProvider_FailsOptionsValidation() {
        var services = new ServiceCollection();
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["Billing:Provider"] = "unsupported",
        });

        services.AddIntegrations(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        OptionsValidationException ex = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<BillingOptions>>().Value);
        Assert.Contains("supported billing provider", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddIntegrations_WithIncompleteStripeConfiguration_FailsOptionsValidation() {
        var services = new ServiceCollection();
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["Stripe:SecretKey"] = "sk_test",
            ["Stripe:WebhookSecret"] = "whsec_test",
            ["Stripe:PremiumMonthlyPriceId"] = "price_monthly",
            ["Stripe:PremiumYearlyPriceId"] = "price_yearly",
            ["Stripe:SuccessUrl"] = "not-a-url",
            ["Stripe:CancelUrl"] = "https://example.com/cancel",
            ["Stripe:PortalReturnUrl"] = "https://example.com/portal",
        });

        services.AddIntegrations(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        OptionsValidationException ex = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<StripeOptions>>().Value);
        Assert.Contains("Stripe configuration is incomplete", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddIntegrations_WithOnlyLegacyStripePublishableKey_DoesNotRequireStripeConfiguration() {
        var services = new ServiceCollection();
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["Stripe:PublishableKey"] = "pk_legacy",
        });

        services.AddIntegrations(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        StripeOptions options = provider.GetRequiredService<IOptions<StripeOptions>>().Value;
        Assert.Equal("pk_legacy", options.PublishableKey);
    }

    [Fact]
    public void AddIntegrations_WithInvalidWebPushConfiguration_FailsOptionsValidation() {
        var services = new ServiceCollection();
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["WebPush:Enabled"] = "true",
            ["WebPush:Subject"] = "not-a-url",
            ["WebPush:PublicKey"] = "public",
            ["WebPush:PrivateKey"] = "private",
        });

        services.AddIntegrations(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        OptionsValidationException ex = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<WebPushOptions>>().Value);
        Assert.Contains("WebPush configuration is invalid", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddIntegrations_WithOpenAiApiKeyAndMissingTextModel_FailsOptionsValidation() {
        var services = new ServiceCollection();
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["OpenAi:ApiKey"] = "test-key",
            ["OpenAi:TextModel"] = "",
            ["OpenAi:VisionModel"] = "vision-model",
            ["OpenAi:VisionFallbackModel"] = "vision-fallback",
        });

        services.AddIntegrations(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        OptionsValidationException ex = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<OpenAiOptions>>().Value);
        Assert.Contains("TextModel", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddInfrastructure_RegistersDatabaseCommandTelemetryInterceptor() {
        var services = new ServiceCollection();
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=food_diary;Username=test;Password=test",
            ["Jwt:SecretKey"] = "super-secret-key-for-tests-only-123456789",
            ["Jwt:Issuer"] = "FoodDiary",
            ["Jwt:Audience"] = "FoodDiaryClients",
            ["Jwt:ExpirationMinutes"] = "60",
            ["Jwt:RefreshTokenExpirationDays"] = "7",
            ["Jwt:RememberMeRefreshTokenExpirationDays"] = "90",
        });

        services.AddInfrastructure(configuration);

        ServiceDescriptor interceptorDescriptor = Assert.Single(
            services,
            static descriptor => descriptor.ServiceType == typeof(DatabaseCommandTelemetryInterceptor));
        Assert.Equal(ServiceLifetime.Singleton, interceptorDescriptor.Lifetime);
    }

    [Fact]
    public void AddInfrastructure_DietologistRepositoryAliasesResolveThroughScopedConcreteInstances() {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IPublisher>());
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=food_diary;Username=test;Password=test",
            ["Jwt:SecretKey"] = "super-secret-key-for-tests-only-123456789",
            ["Jwt:Issuer"] = "FoodDiary",
            ["Jwt:Audience"] = "FoodDiaryClients",
            ["Jwt:ExpirationMinutes"] = "60",
            ["Jwt:RefreshTokenExpirationDays"] = "7",
            ["Jwt:RememberMeRefreshTokenExpirationDays"] = "90",
        });
        services.AddInfrastructure(configuration);

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        IServiceProvider scoped = scope.ServiceProvider;

        Assert.Multiple(
            () => Assert.Same(scoped.GetRequiredService<IRecommendationCommentRepository>(), scoped.GetRequiredService<IRecommendationCommentWriteRepository>()),
            () => Assert.Same(scoped.GetRequiredService<IRecommendationCommentRepository>(), scoped.GetRequiredService<IRecommendationCommentReadModelRepository>()),
            () => Assert.Same(scoped.GetRequiredService<IClientTaskRepository>(), scoped.GetRequiredService<IClientTaskWriteRepository>()),
            () => Assert.Same(scoped.GetRequiredService<IClientTaskRepository>(), scoped.GetRequiredService<IClientTaskReadModelRepository>()),
            () => Assert.Same(scoped.GetRequiredService<IRecommendationTemplateRepository>(), scoped.GetRequiredService<IRecommendationTemplateWriteRepository>()),
            () => Assert.Same(scoped.GetRequiredService<IRecommendationTemplateRepository>(), scoped.GetRequiredService<IRecommendationTemplateReadModelRepository>()),
            () => Assert.Same(scoped.GetRequiredService<IRecommendationBulkDispatchRepository>(), scoped.GetRequiredService<IRecommendationBulkDispatchLookupRepository>()),
            () => Assert.Same(scoped.GetRequiredService<IRecommendationBulkDispatchRepository>(), scoped.GetRequiredService<IRecommendationBulkDispatchWriteRepository>()),
            () => Assert.IsType<AttentionSignalMetricsReadService>(scoped.GetRequiredService<IAttentionSignalMetricsReadService>()));
    }

    [Fact]
    public void AddInfrastructure_DashboardReadServicesResolveThroughScopedConcreteInstances() {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IPublisher>());
        services.AddScoped<IDashboardStatisticsReadService>(_ => Substitute.For<IDashboardStatisticsReadService>());
        services.AddScoped<IDashboardBodyReadService>(_ => Substitute.For<IDashboardBodyReadService>());
        services.AddScoped<IDashboardMealsReadService>(_ => Substitute.For<IDashboardMealsReadService>());
        services.AddScoped<IDashboardReadService>(_ => Substitute.For<IDashboardReadService>());
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=food_diary;Username=test;Password=test",
            ["Jwt:SecretKey"] = "super-secret-key-for-tests-only-123456789",
            ["Jwt:Issuer"] = "FoodDiary",
            ["Jwt:Audience"] = "FoodDiaryClients",
            ["Jwt:ExpirationMinutes"] = "60",
            ["Jwt:RefreshTokenExpirationDays"] = "7",
            ["Jwt:RememberMeRefreshTokenExpirationDays"] = "90",
        });

        services.AddInfrastructure(configuration);
        Assert.Multiple(
            () => Assert.Equal(1, services.Count(static descriptor => descriptor.ServiceType == typeof(IDashboardStatisticsReadService))),
            () => Assert.Equal(1, services.Count(static descriptor => descriptor.ServiceType == typeof(IDashboardBodyReadService))),
            () => Assert.Equal(1, services.Count(static descriptor => descriptor.ServiceType == typeof(IDashboardMealsReadService))),
            () => Assert.Equal(1, services.Count(static descriptor => descriptor.ServiceType == typeof(IDashboardReadService))));

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        Assert.Multiple(
            () => Assert.Same(
                scope.ServiceProvider.GetRequiredService<DashboardStatisticsReadService>(),
                scope.ServiceProvider.GetRequiredService<IDashboardStatisticsReadService>()),
            () => Assert.Same(
                scope.ServiceProvider.GetRequiredService<DashboardBodyReadService>(),
                scope.ServiceProvider.GetRequiredService<IDashboardBodyReadService>()),
            () => Assert.Same(
                scope.ServiceProvider.GetRequiredService<DashboardMealsReadService>(),
                scope.ServiceProvider.GetRequiredService<IDashboardMealsReadService>()));
    }

    [Fact]
    public void AddInfrastructure_TrackingRepositoriesResolveThroughScopedConcreteInstances() {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IPublisher>());
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=food_diary;Username=test;Password=test",
            ["Jwt:SecretKey"] = "super-secret-key-for-tests-only-123456789",
            ["Jwt:Issuer"] = "FoodDiary",
            ["Jwt:Audience"] = "FoodDiaryClients",
            ["Jwt:ExpirationMinutes"] = "60",
            ["Jwt:RefreshTokenExpirationDays"] = "7",
            ["Jwt:RememberMeRefreshTokenExpirationDays"] = "90",
        });

        services.AddInfrastructure(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        IWeightEntryRepository weightRepository = scope.ServiceProvider.GetRequiredService<IWeightEntryRepository>();
        IWaistEntryRepository waistRepository = scope.ServiceProvider.GetRequiredService<IWaistEntryRepository>();
        IHydrationEntryReadModelRepository hydrationRepository = scope.ServiceProvider.GetRequiredService<IHydrationEntryReadModelRepository>();
        IDailyAdviceReadModelRepository dailyAdviceRepository = scope.ServiceProvider.GetRequiredService<IDailyAdviceReadModelRepository>();
        ICycleRepository cycleRepository = scope.ServiceProvider.GetRequiredService<ICycleRepository>();
        IExerciseEntryRepository exerciseRepository = scope.ServiceProvider.GetRequiredService<IExerciseEntryRepository>();

        Assert.Multiple(
            () => Assert.IsType<WeightEntryRepository>(weightRepository),
            () => Assert.Same(weightRepository, scope.ServiceProvider.GetRequiredService<IWeightEntryReadRepository>()),
            () => Assert.Same(weightRepository, scope.ServiceProvider.GetRequiredService<IWeightEntryWriteRepository>()),
            () => Assert.IsType<WaistEntryRepository>(waistRepository),
            () => Assert.Same(waistRepository, scope.ServiceProvider.GetRequiredService<IWaistEntryReadRepository>()),
            () => Assert.Same(waistRepository, scope.ServiceProvider.GetRequiredService<IWaistEntryWriteRepository>()),
            () => Assert.IsType<HydrationEntryRepository>(hydrationRepository),
            () => Assert.Same(hydrationRepository, scope.ServiceProvider.GetRequiredService<IHydrationEntryWriteRepository>()),
            () => Assert.IsType<DailyAdviceRepository>(dailyAdviceRepository),
            () => Assert.IsType<CycleRepository>(cycleRepository),
            () => Assert.Same(cycleRepository, scope.ServiceProvider.GetRequiredService<ICycleReadRepository>()),
            () => Assert.Same(cycleRepository, scope.ServiceProvider.GetRequiredService<ICycleWriteRepository>()),
            () => Assert.IsType<ExerciseEntryRepository>(exerciseRepository),
            () => Assert.Same(exerciseRepository, scope.ServiceProvider.GetRequiredService<IExerciseEntryReadRepository>()),
            () => Assert.Same(exerciseRepository, scope.ServiceProvider.GetRequiredService<IExerciseEntryWriteRepository>()));
    }

    [Theory]
    [MemberData(nameof(SplitRepositoryRegistrationCases))]
    public void AddInfrastructureAndFastingModule_SplitRepositoriesResolveThroughSameScopedInstance(string primaryTypeName, string[] aliasTypeNames) {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IPublisher>());
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=food_diary;Username=test;Password=test",
            ["Jwt:SecretKey"] = "super-secret-key-for-tests-only-123456789",
            ["Jwt:Issuer"] = "FoodDiary",
            ["Jwt:Audience"] = "FoodDiaryClients",
            ["Jwt:ExpirationMinutes"] = "60",
            ["Jwt:RefreshTokenExpirationDays"] = "7",
            ["Jwt:RememberMeRefreshTokenExpirationDays"] = "90",
        });

        services.AddInfrastructure(configuration).AddFastingModule();
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        object primary = scope.ServiceProvider.GetRequiredService(FindType(primaryTypeName));

        Assert.Multiple([.. aliasTypeNames.Select<string, Action>(aliasTypeName => () =>
            Assert.Same(primary, scope.ServiceProvider.GetRequiredService(FindType(aliasTypeName))))]);
    }

    [Fact]
    public void AddInfrastructure_CanResolveDiaryPdfGeneratorTypedClient() {
        var services = new ServiceCollection();
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=food_diary;Username=test;Password=test",
            ["Jwt:SecretKey"] = "super-secret-key-for-tests-only-123456789",
            ["Jwt:Issuer"] = "FoodDiary",
            ["Jwt:Audience"] = "FoodDiaryClients",
            ["Jwt:ExpirationMinutes"] = "60",
            ["Jwt:RefreshTokenExpirationDays"] = "7",
            ["Jwt:RememberMeRefreshTokenExpirationDays"] = "90",
        });

        services.AddSingleton<IDiaryPdfReportTextProvider, TestDiaryPdfReportTextProvider>();
        services.AddInfrastructure(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        IDiaryPdfGenerator generator = provider.GetRequiredService<IDiaryPdfGenerator>();

        Assert.NotNull(generator);
    }

    [Fact]
    public async Task ConnectToAllowedRemoteImageEndpointAsync_WhenHostResolvesToLoopback_RejectsConnection() {
        SocketsHttpConnectionContext context = CreateSocketsHttpConnectionContext(
            new DnsEndPoint("localhost", 80),
            new HttpRequestMessage(HttpMethod.Get, "http://localhost/"));

        HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(async () => {
            await InvokePrivateStatic<ValueTask<Stream>>(
                "ConnectToAllowedRemoteImageEndpointAsync",
                context,
                CancellationToken.None).ConfigureAwait(true);
        }).ConfigureAwait(true);

        Assert.Contains("private or loopback", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConnectToAllowedRemoteImageEndpointAsync_WhenHostResolvesToPublicAddress_UsesSocketConnector() {
        Func<string, CancellationToken, ValueTask<IPAddress[]>> originalResolver =
            FoodDiary.Infrastructure.DependencyInjection.ResolveRemoteImageHostAddressesAsync;
        Func<IPAddress, int, CancellationToken, ValueTask<Stream>> originalConnector =
            FoodDiary.Infrastructure.DependencyInjection.ConnectRemoteImageSocketAsync;
        try {
            FoodDiary.Infrastructure.DependencyInjection.ResolveRemoteImageHostAddressesAsync =
                static (_, _) => ValueTask.FromResult<IPAddress[]>([IPAddress.Parse("8.8.8.8")]);
            FoodDiary.Infrastructure.DependencyInjection.ConnectRemoteImageSocketAsync =
                static (address, port, _) => {
                    Assert.Equal(IPAddress.Parse("8.8.8.8"), address);
                    Assert.Equal(443, port);
                    return ValueTask.FromResult<Stream>(new MemoryStream([1, 2, 3]));
                };
            SocketsHttpConnectionContext context = CreateSocketsHttpConnectionContext(
                new DnsEndPoint("push.example.com", 443),
                new HttpRequestMessage(HttpMethod.Get, "https://push.example.com/"));

            await using Stream stream = await InvokePrivateStatic<ValueTask<Stream>>(
                "ConnectToAllowedRemoteImageEndpointAsync",
                context,
                CancellationToken.None);

            Assert.Equal(3, stream.Length);
        } finally {
            FoodDiary.Infrastructure.DependencyInjection.ResolveRemoteImageHostAddressesAsync = originalResolver;
            FoodDiary.Infrastructure.DependencyInjection.ConnectRemoteImageSocketAsync = originalConnector;
        }
    }

    [Theory]
    [InlineData("192.0.0.1")]
    [InlineData("198.18.0.1")]
    public async Task ConnectToAllowedRemoteImageEndpointAsync_WhenReboundToSpecialUseAddress_RejectsConnection(string address) {
        Func<string, CancellationToken, ValueTask<IPAddress[]>> originalResolver =
            FoodDiary.Infrastructure.DependencyInjection.ResolveRemoteImageHostAddressesAsync;
        Func<IPAddress, int, CancellationToken, ValueTask<Stream>> originalConnector =
            FoodDiary.Infrastructure.DependencyInjection.ConnectRemoteImageSocketAsync;
        bool connectorCalled = false;
        try {
            FoodDiary.Infrastructure.DependencyInjection.ResolveRemoteImageHostAddressesAsync =
                (_, _) => ValueTask.FromResult<IPAddress[]>([IPAddress.Parse(address)]);
            FoodDiary.Infrastructure.DependencyInjection.ConnectRemoteImageSocketAsync = (_, _, _) => {
                connectorCalled = true;
                return ValueTask.FromResult<Stream>(new MemoryStream());
            };
            SocketsHttpConnectionContext context = CreateSocketsHttpConnectionContext(
                new DnsEndPoint("rebound.example.com", 443),
                new HttpRequestMessage(HttpMethod.Get, "https://rebound.example.com/image.png"));

            await Assert.ThrowsAsync<HttpRequestException>(async () => {
                await InvokePrivateStatic<ValueTask<Stream>>(
                    "ConnectToAllowedRemoteImageEndpointAsync",
                    context,
                    CancellationToken.None).ConfigureAwait(true);
            }).ConfigureAwait(true);

            Assert.False(connectorCalled);
        } finally {
            FoodDiary.Infrastructure.DependencyInjection.ResolveRemoteImageHostAddressesAsync = originalResolver;
            FoodDiary.Infrastructure.DependencyInjection.ConnectRemoteImageSocketAsync = originalConnector;
        }
    }

    [Fact]
    public async Task ConnectRemoteImageSocketAsync_WhenListenerAcceptsConnection_ReturnsNetworkStream() {
        var listener = new TcpListener(IPAddress.Loopback, port: 0);
        listener.Start();
        try {
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            ValueTask<Stream> streamTask = FoodDiary.Infrastructure.DependencyInjection.ConnectRemoteImageSocketAsync(
                IPAddress.Loopback,
                port,
                CancellationToken.None);
            using TcpClient client = await listener.AcceptTcpClientAsync();
            await using Stream stream = await streamTask;

            Assert.True(stream.CanRead);
        } finally {
            listener.Stop();
        }
    }

    [Fact]
    public async Task ConnectRemoteImageSocketAsync_WhenConnectionFails_DisposesSocketAndThrows() {
        var listener = new TcpListener(IPAddress.Loopback, port: 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await Assert.ThrowsAnyAsync<SocketException>(async () => {
            await FoodDiary.Infrastructure.DependencyInjection.ConnectRemoteImageSocketAsync(
                IPAddress.Loopback,
                port,
                cancellationTokenSource.Token).ConfigureAwait(false);
        });
    }

    [Theory]
    [InlineData("8.8.8.8", true)]
    [InlineData("127.0.0.1", false)]
    [InlineData("0.0.0.0", false)]
    [InlineData("255.255.255.255", false)]
    [InlineData("10.1.2.3", false)]
    [InlineData("172.16.0.1", false)]
    [InlineData("172.31.255.255", false)]
    [InlineData("172.32.0.1", true)]
    [InlineData("192.168.1.1", false)]
    [InlineData("192.0.0.1", false)]
    [InlineData("198.18.0.1", false)]
    [InlineData("198.19.255.255", false)]
    [InlineData("169.254.1.1", false)]
    [InlineData("100.64.0.1", false)]
    [InlineData("100.127.255.255", false)]
    [InlineData("100.128.0.1", true)]
    [InlineData("224.0.0.1", false)]
    [InlineData("::1", false)]
    [InlineData("::", false)]
    [InlineData("2001:4860:4860::8888", true)]
    [InlineData("fe80::1", false)]
    [InlineData("fec0::1", false)]
    [InlineData("fc00::1", false)]
    [InlineData("ff02::1", false)]
    [InlineData("::ffff:8.8.8.8", true)]
    [InlineData("::ffff:10.1.2.3", false)]
    public void IsPublicAddress_ReturnsExpectedResult(string address, bool expected) {
        bool result = InvokePrivateStatic<bool>("IsPublicAddress", IPAddress.Parse(address));

        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsPublicAddressCore_WhenAddressFamilyIsUnsupported_ReturnsFalse() {
        bool result = FoodDiary.Infrastructure.DependencyInjection.IsPublicAddressCore(
            AddressFamily.Unknown,
            [1, 2, 3, 4],
            isIPv6LinkLocal: false,
            isIPv6SiteLocal: false,
            isIPv6Multicast: false);

        Assert.False(result);
    }

    [Fact]
    public void AddInfrastructure_WithInvalidDatabaseRetryCount_FailsOptionsValidation() {
        var services = new ServiceCollection();
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=food_diary;Username=test;Password=test",
            ["Jwt:SecretKey"] = "super-secret-key-for-tests-only-123456789",
            ["Jwt:Issuer"] = "FoodDiary",
            ["Jwt:Audience"] = "FoodDiaryClients",
            ["Jwt:ExpirationMinutes"] = "60",
            ["Jwt:RefreshTokenExpirationDays"] = "7",
            ["Jwt:RememberMeRefreshTokenExpirationDays"] = "90",
            ["Database:EnableRetries"] = "true",
            ["Database:MaxRetryCount"] = "0",
        });

        services.AddInfrastructure(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        OptionsValidationException ex = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<DatabaseOptions>>().Value);
        Assert.Contains("MaxRetryCount", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddInfrastructure_WhenOutboxLeaseDoesNotCoverProcessingBudgets_FailsOptionsValidation() {
        var services = new ServiceCollection();
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=food_diary;Username=test;Password=test",
            ["Jwt:SecretKey"] = "super-secret-key-for-tests-only-123456789",
            ["Jwt:Issuer"] = "FoodDiary",
            ["Jwt:Audience"] = "FoodDiaryClients",
            ["Jwt:ExpirationMinutes"] = "60",
            ["Jwt:RefreshTokenExpirationDays"] = "7",
            ["Jwt:RememberMeRefreshTokenExpirationDays"] = "90",
            ["OutboxProcessing:LeaseDuration"] = "00:01:00",
            ["OutboxProcessing:DispatchTimeout"] = "00:00:50",
            ["OutboxProcessing:FinalizationTimeout"] = "00:00:10",
        });

        services.AddInfrastructure(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<OutboxProcessingOptions>>().Value);
        Assert.Contains("LeaseDuration", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddInfrastructure_WithRetriesEnabled_ConfiguresRetryingExecutionStrategy() {
        var services = new ServiceCollection();
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=food_diary;Username=test;Password=test",
            ["Jwt:SecretKey"] = "super-secret-key-for-tests-only-123456789",
            ["Jwt:Issuer"] = "FoodDiary",
            ["Jwt:Audience"] = "FoodDiaryClients",
            ["Jwt:ExpirationMinutes"] = "60",
            ["Jwt:RefreshTokenExpirationDays"] = "7",
            ["Jwt:RememberMeRefreshTokenExpirationDays"] = "90",
            ["Database:EnableRetries"] = "true",
            ["Database:MaxRetryCount"] = "4",
            ["Database:MaxRetryDelaySeconds"] = "7",
        });

        services.AddSingleton<IPublisher>(new NullPublisher());
        services.AddInfrastructure(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        using FoodDiaryDbContext context = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();

        IExecutionStrategy strategy = context.Database.CreateExecutionStrategy();

        Assert.Contains("Retry", strategy.GetType().Name, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FoodDiaryDbContext_Model_ConfiguresCascadeDeleteForAllUserOwnedEntities() {
        DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseNpgsql("Host=localhost;Database=food_diary;Username=test;Password=test")
            .Options;

        using var context = new FoodDiaryDbContext(options);
        var failures = GetUserOwnedEntityTypes()
            .Select(entityType => ValidateUserForeignKey(context, entityType))
            .Where(message => message is not null)
            .ToList();

        Assert.True(
            failures.Count == 0,
            "Missing or invalid User FK mappings:" + Environment.NewLine + string.Join(Environment.NewLine, failures!));
    }

    private static IConfiguration CreateConfiguration(Dictionary<string, string?> values) {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private static IConfiguration CreateValidIntegrationsConfiguration() {
        VapidDetails vapidKeys = VapidHelper.GenerateVapidKeys();
        return CreateConfiguration(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["S3:AccessKeyId"] = "access",
            ["S3:SecretAccessKey"] = "secret",
            ["S3:Region"] = "eu-central-1",
            ["S3:Bucket"] = "food-diary-test",
            ["S3:StagingBucket"] = "food-diary-test-staging",
            ["S3:ServiceUrl"] = "https://s3.example.com",
            ["S3:PublicBaseUrl"] = "https://cdn.example.com",
            ["S3:AllowPublicImageAccess"] = "true",
            ["S3:MaxUploadSizeBytes"] = "1048576",
            ["OpenAi:ApiKey"] = "test-key",
            ["OpenAi:VisionModel"] = "vision-model",
            ["OpenAi:VisionFallbackModel"] = "vision-fallback",
            ["OpenAi:TextModel"] = "text-model",
            ["GoogleAuth:ClientId"] = "google-client",
            ["Billing:Provider"] = BillingProviderNames.Stripe,
            ["Stripe:SecretKey"] = "sk_test",
            ["Stripe:PublishableKey"] = "pk_test",
            ["Stripe:WebhookSecret"] = "whsec_test",
            ["Stripe:PremiumMonthlyPriceId"] = "price_monthly",
            ["Stripe:PremiumYearlyPriceId"] = "price_yearly",
            ["Stripe:SuccessUrl"] = "https://example.com/success",
            ["Stripe:CancelUrl"] = "https://example.com/cancel",
            ["Stripe:PortalReturnUrl"] = "https://example.com/portal",
            ["Paddle:ApiKey"] = "paddle-key",
            ["Paddle:ApiBaseUrl"] = "https://sandbox-api.paddle.com",
            ["Paddle:ClientSideToken"] = "test_paddle-client-token",
            ["Paddle:WebhookSecretKey"] = "paddle-secret",
            ["Paddle:NotificationSettingId"] = "ntfset_01abcdefghijklmnopqrstuvwx",
            ["Paddle:PremiumMonthlyPriceId"] = "paddle-monthly",
            ["Paddle:PremiumYearlyPriceId"] = "paddle-yearly",
            ["Paddle:CheckoutUrl"] = "https://example.com/checkout",
            ["YooKassa:ShopId"] = "shop",
            ["YooKassa:SecretKey"] = "secret",
            ["YooKassa:PremiumMonthlyAmount"] = "199.00",
            ["YooKassa:PremiumYearlyAmount"] = "1990.00",
            ["YooKassa:ReturnUrl"] = "https://example.com/return",
            ["WebPush:Enabled"] = "true",
            ["WebPush:Subject"] = "https://example.com",
            ["WebPush:PublicKey"] = vapidKeys.PublicKey,
            ["WebPush:PrivateKey"] = vapidKeys.PrivateKey,
            ["WebPush:DefaultUrl"] = "/",
            ["MailRelayClient:BaseUrl"] = "https://mail-relay.example.com",
            ["MailRelayClient:ApiKey"] = "relay-key",
            ["MailInboxClient:BaseUrl"] = "https://mail-inbox.example.com",
            ["MailInboxClient:MetadataApiKey"] = "fedcba9876543210fedcba987654321a",
            ["MailInboxClient:ContentApiKey"] = "fedcba9876543210fedcba987654321b",
            ["MailInboxClient:StateApiKey"] = "fedcba9876543210fedcba987654321c",
            ["UsdaApi:ApiKey"] = "usda-key",
            ["OpenFoodFacts:UserAgent"] = "FoodDiaryTests/1.0",
            ["Fitbit:ClientId"] = "fitbit-client",
            ["Fitbit:ClientSecret"] = "fitbit-secret",
            ["Fitbit:RedirectUri"] = "https://example.com/fitbit",
        });
    }

    public static TheoryData<string, string[]> SplitRepositoryRegistrationCases() => new() {
        {
            "FoodDiary.Infrastructure.Persistence.ContentReports.ContentReportRepository",
            [
                "FoodDiary.Application.Abstractions.ContentReports.Common.IContentReportReadModelRepository",
                "FoodDiary.Application.Abstractions.ContentReports.Common.IContentReportWriteRepository",
            ]
        },
        {
            "FoodDiary.Infrastructure.Persistence.Audit.AuditEntryService",
            [
                "FoodDiary.Application.Abstractions.Audit.Common.IAuditEntryReadService",
                "FoodDiary.Application.Abstractions.Audit.Common.IAuditEntryWriter",
            ]
        },
        {
            "FoodDiary.Application.Abstractions.Products.Common.IProductRepository",
            [
                "FoodDiary.Application.Abstractions.Products.Common.IProductReadRepository",
                "FoodDiary.Application.Abstractions.Products.Common.IProductWriteRepository",
            ]
        },
        {
            "FoodDiary.Application.Abstractions.Recipes.Common.IRecipeRepository",
            [
                "FoodDiary.Application.Abstractions.Recipes.Common.IRecipeReadRepository",
                "FoodDiary.Application.Abstractions.Recipes.Common.IRecipeWriteRepository",
                "FoodDiary.Application.Abstractions.Recipes.Common.IRecipeNutritionWriter",
            ]
        },
        {
            "FoodDiary.Application.Abstractions.RecentItems.Common.IRecentItemRepository",
            [
                "FoodDiary.Application.Abstractions.RecentItems.Common.IRecentItemReadRepository",
                "FoodDiary.Application.Abstractions.RecentItems.Common.IRecentItemWriteRepository",
            ]
        },
        {
            "FoodDiary.Application.Abstractions.Meals.Common.IMealRepository",
            [
                "FoodDiary.Application.Abstractions.Meals.Common.IMealReadRepository",
                "FoodDiary.Application.Abstractions.Meals.Common.IMealProjectionReadRepository",
                "FoodDiary.Application.Abstractions.Meals.Common.IMealActivityReadRepository",
                "FoodDiary.Application.Abstractions.Meals.Common.IMealProductNutritionReadRepository",
                "FoodDiary.Application.Abstractions.Meals.Common.IMealWriteRepository",
            ]
        },
        {
            "FoodDiary.Application.Abstractions.FavoriteMeals.Common.IFavoriteMealRepository",
            [
                "FoodDiary.Application.Abstractions.FavoriteMeals.Common.IFavoriteMealReadRepository",
                "FoodDiary.Application.Abstractions.FavoriteMeals.Common.IFavoriteMealReadModelRepository",
                "FoodDiary.Application.Abstractions.FavoriteMeals.Common.IFavoriteMealWriteRepository",
            ]
        },
        {
            "FoodDiary.Application.Abstractions.FavoriteProducts.Common.IFavoriteProductRepository",
            [
                "FoodDiary.Application.Abstractions.FavoriteProducts.Common.IFavoriteProductReadRepository",
                "FoodDiary.Application.Abstractions.FavoriteProducts.Common.IFavoriteProductReadModelRepository",
                "FoodDiary.Application.Abstractions.FavoriteProducts.Common.IFavoriteProductWriteRepository",
            ]
        },
        {
            "FoodDiary.Application.Abstractions.FavoriteRecipes.Common.IFavoriteRecipeRepository",
            [
                "FoodDiary.Application.Abstractions.FavoriteRecipes.Common.IFavoriteRecipeReadRepository",
                "FoodDiary.Application.Abstractions.FavoriteRecipes.Common.IFavoriteRecipeReadModelRepository",
                "FoodDiary.Application.Abstractions.FavoriteRecipes.Common.IFavoriteRecipeWriteRepository",
            ]
        },
        {
            "FoodDiary.Application.Abstractions.Dietologist.Common.IDietologistInvitationRepository",
            [
                "FoodDiary.Application.Abstractions.Dietologist.Common.IDietologistInvitationReadRepository",
                "FoodDiary.Application.Abstractions.Dietologist.Common.IDietologistInvitationReadModelRepository",
                "FoodDiary.Application.Abstractions.Dietologist.Common.IDietologistInvitationWriteRepository",
            ]
        },
        {
            "FoodDiary.Application.Abstractions.Dietologist.Common.IRecommendationRepository",
            [
                "FoodDiary.Application.Abstractions.Dietologist.Common.IRecommendationReadRepository",
                "FoodDiary.Application.Abstractions.Dietologist.Common.IRecommendationReadModelRepository",
                "FoodDiary.Application.Abstractions.Dietologist.Common.IRecommendationWriteRepository",
            ]
        },
        {
            "FoodDiary.Application.Abstractions.RecipeComments.Common.IRecipeCommentRepository",
            [
                "FoodDiary.Application.Abstractions.RecipeComments.Common.IRecipeCommentReadRepository",
                "FoodDiary.Application.Abstractions.RecipeComments.Common.IRecipeCommentReadModelRepository",
                "FoodDiary.Application.Abstractions.RecipeComments.Common.IRecipeCommentWriteRepository",
            ]
        },
        {
            "FoodDiary.Application.Abstractions.RecipeLikes.Common.IRecipeLikeRepository",
            [
                "FoodDiary.Application.Abstractions.RecipeLikes.Common.IRecipeLikeReadRepository",
                "FoodDiary.Application.Abstractions.RecipeLikes.Common.IRecipeLikeWriteRepository",
            ]
        },
        {
            "FoodDiary.Application.Abstractions.Usda.Common.IUsdaFoodRepository",
            [
                "FoodDiary.Application.Abstractions.Usda.Common.IUsdaFoodReadRepository",
                "FoodDiary.Application.Abstractions.Usda.Common.IUsdaFoodReadModelRepository",
            ]
        },
        {
            "FoodDiary.Application.Abstractions.OpenFoodFacts.Common.IOpenFoodFactsProductCacheRepository",
            [
                "FoodDiary.Application.Abstractions.OpenFoodFacts.Common.IOpenFoodFactsProductCacheReadRepository",
                "FoodDiary.Application.Abstractions.OpenFoodFacts.Common.IOpenFoodFactsProductCacheWriteRepository",
            ]
        },
        {
            "FoodDiary.Application.Abstractions.Lessons.Common.INutritionLessonRepository",
            [
                "FoodDiary.Application.Abstractions.Lessons.Common.INutritionLessonReadRepository",
                "FoodDiary.Application.Abstractions.Lessons.Common.INutritionLessonReadModelRepository",
                "FoodDiary.Application.Abstractions.Lessons.Common.INutritionLessonWriteRepository",
            ]
        },
        {
            "FoodDiary.Application.Abstractions.MealPlans.Common.IMealPlanRepository",
            [
                "FoodDiary.Application.Abstractions.MealPlans.Common.IMealPlanReadRepository",
                "FoodDiary.Application.Abstractions.MealPlans.Common.IMealPlanReadModelRepository",
                "FoodDiary.Application.Abstractions.MealPlans.Common.IMealPlanWriteRepository",
            ]
        },
        {
            "FoodDiary.Application.Abstractions.Billing.Common.IBillingSubscriptionRepository",
            [
                "FoodDiary.Application.Abstractions.Billing.Common.IBillingSubscriptionReadRepository",
                "FoodDiary.Application.Abstractions.Billing.Common.IBillingSubscriptionReadModelRepository",
                "FoodDiary.Application.Abstractions.Billing.Common.IBillingSubscriptionWriteRepository",
            ]
        },
        {
            "FoodDiary.Application.Abstractions.Billing.Common.IBillingPaymentRepository",
            [
                "FoodDiary.Application.Abstractions.Billing.Common.IBillingPaymentReadRepository",
                "FoodDiary.Application.Abstractions.Billing.Common.IBillingPaymentWriteRepository",
            ]
        },
        {
            "FoodDiary.Application.Abstractions.Billing.Common.IBillingWebhookEventRepository",
            [
                "FoodDiary.Application.Abstractions.Billing.Common.IBillingWebhookEventReadRepository",
                "FoodDiary.Application.Abstractions.Billing.Common.IBillingWebhookEventWriteRepository",
            ]
        },
        {
            "FoodDiary.Application.Abstractions.Ai.Common.IAiPromptTemplateRepository",
            [
                "FoodDiary.Application.Abstractions.Ai.Common.IAiPromptTemplateReadRepository",
                "FoodDiary.Application.Abstractions.Ai.Common.IAiPromptTemplateReadModelRepository",
                "FoodDiary.Application.Abstractions.Ai.Common.IAiPromptTemplateWriteRepository",
            ]
        },
        {
            "FoodDiary.Application.Abstractions.Fasting.Common.IFastingPlanRepository",
            [
                "FoodDiary.Application.Abstractions.Fasting.Common.IFastingPlanReadRepository",
                "FoodDiary.Application.Abstractions.Fasting.Common.IFastingPlanWriteRepository",
            ]
        },
        {
            "FoodDiary.Application.Abstractions.Fasting.Common.IFastingSessionRepository",
            [
                "FoodDiary.Application.Abstractions.Fasting.Common.IFastingSessionReadRepository",
                "FoodDiary.Application.Abstractions.Fasting.Common.IFastingSessionWriteRepository",
            ]
        },
        {
            "FoodDiary.Application.Abstractions.Fasting.Common.IFastingTelemetryEventRepository",
            [
                "FoodDiary.Application.Abstractions.Fasting.Common.IFastingTelemetryEventReadRepository",
                "FoodDiary.Application.Abstractions.Fasting.Common.IFastingTelemetryEventWriteRepository",
            ]
        },
        {
            "FoodDiary.Application.Abstractions.ShoppingLists.Common.IShoppingListRepository",
            [
                "FoodDiary.Application.Abstractions.ShoppingLists.Common.IShoppingListReadRepository",
                "FoodDiary.Application.Abstractions.ShoppingLists.Common.IShoppingListReadModelRepository",
                "FoodDiary.Application.Abstractions.ShoppingLists.Common.IShoppingListWriteRepository",
            ]
        },
        {
            "FoodDiary.Application.Abstractions.WaistEntries.Common.IWaistEntryRepository",
            [
                "FoodDiary.Application.Abstractions.WaistEntries.Common.IWaistEntryReadRepository",
                "FoodDiary.Application.Abstractions.WaistEntries.Common.IWaistEntryReadModelRepository",
                "FoodDiary.Application.Abstractions.WaistEntries.Common.IWaistEntryWriteRepository",
            ]
        },
        {
            "FoodDiary.Application.Abstractions.Wearables.Common.IWearableConnectionRepository",
            [
                "FoodDiary.Application.Abstractions.Wearables.Common.IWearableConnectionReadRepository",
                "FoodDiary.Application.Abstractions.Wearables.Common.IWearableConnectionWriteRepository",
            ]
        },
        {
            "FoodDiary.Application.Abstractions.Wearables.Common.IWearableSyncRepository",
            [
                "FoodDiary.Application.Abstractions.Wearables.Common.IWearableSyncReadRepository",
                "FoodDiary.Application.Abstractions.Wearables.Common.IWearableSyncReadModelRepository",
                "FoodDiary.Application.Abstractions.Wearables.Common.IWearableSyncWriteRepository",
            ]
        },
        {
            "FoodDiary.Application.Abstractions.Authentication.Common.IUserLoginEventRepository",
            [
                "FoodDiary.Application.Abstractions.Authentication.Common.IUserLoginEventReadRepository",
                "FoodDiary.Application.Abstractions.Authentication.Common.IUserLoginEventWriteRepository",
            ]
        },
        {
            "FoodDiary.Application.Abstractions.Marketing.Common.IMarketingAttributionEventRepository",
            [
                "FoodDiary.Application.Abstractions.Marketing.Common.IMarketingAttributionEventReadRepository",
                "FoodDiary.Application.Abstractions.Marketing.Common.IMarketingAttributionEventWriteRepository",
            ]
        },
        {
            "FoodDiary.Application.Abstractions.Authentication.Common.IRefreshTokenSessionRepository",
            [
                "FoodDiary.Application.Abstractions.Authentication.Common.IRefreshTokenSessionReadRepository",
                "FoodDiary.Application.Abstractions.Authentication.Common.IRefreshTokenSessionWriteRepository",
            ]
        },
        {
            "FoodDiary.Application.Abstractions.Admin.Common.IAdminBillingRepository",
            [
                "FoodDiary.Application.Abstractions.Admin.Common.IAdminBillingReadRepository",
            ]
        },
        {
            "FoodDiary.Application.Abstractions.Admin.Common.IAdminImpersonationSessionRepository",
            [
                "FoodDiary.Application.Abstractions.Admin.Common.IAdminImpersonationSessionReadRepository",
                "FoodDiary.Application.Abstractions.Admin.Common.IAdminImpersonationSessionWriteRepository",
            ]
        },
        {
            "FoodDiary.Application.Abstractions.Admin.Common.IAdminUserRoleAuditRepository",
            [
                "FoodDiary.Application.Abstractions.Admin.Common.IAdminUserRoleAuditReadRepository",
            ]
        },
        {
            "FoodDiary.Application.Abstractions.Admin.Common.IEmailTemplateRepository",
            [
                "FoodDiary.Application.Abstractions.Admin.Common.IEmailTemplateReadRepository",
                "FoodDiary.Application.Abstractions.Admin.Common.IEmailTemplateReadModelRepository",
                "FoodDiary.Application.Abstractions.Admin.Common.IEmailTemplateWriteRepository",
            ]
        },
        {
            "FoodDiary.Application.Abstractions.Notifications.Common.INotificationRepository",
            [
                "FoodDiary.Application.Abstractions.Notifications.Common.INotificationReadRepository",
                "FoodDiary.Application.Abstractions.Notifications.Common.INotificationLookupRepository",
                "FoodDiary.Application.Abstractions.Notifications.Common.INotificationReadModelRepository",
                "FoodDiary.Application.Abstractions.Notifications.Common.INotificationWriteRepository",
            ]
        },
        {
            "FoodDiary.Application.Abstractions.Notifications.Common.IWebPushSubscriptionRepository",
            [
                "FoodDiary.Application.Abstractions.Notifications.Common.IWebPushSubscriptionReadRepository",
                "FoodDiary.Application.Abstractions.Notifications.Common.IWebPushSubscriptionReadModelRepository",
                "FoodDiary.Application.Abstractions.Notifications.Common.IWebPushSubscriptionWriteRepository",
            ]
        },
        {
            "FoodDiary.Application.Abstractions.Users.Common.IUserRepository",
            [
                "FoodDiary.Application.Abstractions.Users.Common.IUserLookupRepository",
                "FoodDiary.Application.Abstractions.Users.Common.IUserAdminReadRepository",
                "FoodDiary.Application.Abstractions.Users.Common.IUserAdminReadModelRepository",
                "FoodDiary.Application.Abstractions.Users.Common.IUserWriteRepository",
            ]
        },
    };

    private static T InvokePrivateStatic<T>(string methodName, params object[] args) {
        MethodInfo method = typeof(FoodDiary.Infrastructure.DependencyInjection).GetMethod(
            methodName,
            BindingFlags.Static | BindingFlags.NonPublic)!;
        return (T)method.Invoke(null, args)!;
    }

    private static Type FindType(string fullName) {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(fullName, throwOnError: false))
            .FirstOrDefault(type => type is not null)
            ?? throw new InvalidOperationException($"Type '{fullName}' was not found.");
    }

    private static SocketsHttpConnectionContext CreateSocketsHttpConnectionContext(
        DnsEndPoint dnsEndPoint,
        HttpRequestMessage request) {
        ConstructorInfo constructor = typeof(SocketsHttpConnectionContext).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            [typeof(DnsEndPoint), typeof(HttpRequestMessage)],
            modifiers: null)!;

        return (SocketsHttpConnectionContext)constructor.Invoke([dnsEndPoint, request]);
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestDiaryPdfReportTextProvider : IDiaryPdfReportTextProvider {
        public DiaryPdfReportTexts GetTexts(string? locale) =>
            new(
                CultureName: "en",
                ReportTitle: "Food Diary Report",
                PeriodLabel: "Period",
                MealsCountLabel: "{0} meals",
                PeriodSummaryTitle: "Period summary",
                TotalCaloriesTitle: "Total calories",
                KcalUnit: "kcal",
                AveragePerDayTitle: "Average per day",
                TotalForPeriodTitle: "Total for period",
                ProteinsTitle: "Proteins",
                FatsTitle: "Fats",
                CarbsTitle: "Carbs",
                FiberTitle: "Fiber",
                GramsUnit: "g",
                GramsProteinsLabel: "g proteins",
                GramsFatsLabel: "g fats",
                GramsCarbsLabel: "g carbs",
                GramsFiberLabel: "g fiber",
                CaloriesByDayTitle: "Calories by day",
                NutrientsByDayTitle: "Nutrients by day",
                MealsTitle: "Meals",
                NoMealsMessage: "No meals recorded in this period.",
                DateColumn: "Date",
                TypeColumn: "Type",
                ItemsColumn: "Items",
                AmountColumn: "Amount",
                KcalColumn: "Kcal",
                ProteinsColumnShort: "Proteins, g",
                FatsColumnShort: "Fats, g",
                CarbsColumnShort: "Carbs, g",
                FiberColumnShort: "Fiber, g",
                SatietyColumn: "Satiety",
                CommentColumn: "Comment",
                BeforeLabel: "Hunger before",
                AfterLabel: "Satiety after",
                OtherMealType: "Other",
                BreakfastMealType: "Breakfast",
                LunchMealType: "Lunch",
                DinnerMealType: "Dinner",
                SnackMealType: "Snack",
                ItemsPrefix: "Items",
                ItemsNotSpecified: "not specified",
                MoreItemsSuffix: "more",
                RecipeFallback: "Recipe",
                ProductFallback: "Product",
                ServingUnit: "serv.",
                GeneratedByPrefix: "Generated by Food Diary - ");
    }

    private static IEnumerable<Type> GetUserOwnedEntityTypes() {
        return typeof(AiUsage).Assembly
            .GetTypes()
            .Where(type =>
                type is { IsClass: true, IsAbstract: false } &&
                type.Namespace?.StartsWith("FoodDiary.Domain.Entities", StringComparison.Ordinal) == true &&
                type.GetProperty("UserId")?.PropertyType == typeof(UserId))
            .OrderBy(type => type.FullName, StringComparer.Ordinal);
    }

    private static string? ValidateUserForeignKey(FoodDiaryDbContext context, Type clrType) {
        IEntityType? entityType = context.Model.FindEntityType(clrType);
        if (entityType is null) {
            return $"{clrType.FullName}: not mapped in FoodDiaryDbContext.";
        }

        IForeignKey? foreignKey = entityType
            .GetForeignKeys()
            .SingleOrDefault(fk => fk.Properties.Any(property => string.Equals(property.Name, "UserId", StringComparison.Ordinal)));

        if (foreignKey is null) {
            return $"{clrType.FullName}: missing FK for UserId.";
        }

        return foreignKey.DeleteBehavior != DeleteBehavior.Cascade
            ? $"{clrType.FullName}: expected DeleteBehavior.Cascade, got {foreignKey.DeleteBehavior}."
            : null;
    }

    [ExcludeFromCodeCoverage]
    private sealed class NullPublisher : IPublisher {
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class SuccessfulHttpMessageHandler : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingLoggerProvider : ILoggerProvider {
        private readonly System.Collections.Concurrent.ConcurrentQueue<string> _messages = new();

        public IReadOnlyCollection<string> Messages => _messages;

        public ILogger CreateLogger(string categoryName) => new RecordingLogger(_messages);

        public void Dispose() {
        }

        [ExcludeFromCodeCoverage]
        private sealed class RecordingLogger(System.Collections.Concurrent.ConcurrentQueue<string> messages) : ILogger {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter) =>
                messages.Enqueue(formatter(state, exception));
        }
    }
}
