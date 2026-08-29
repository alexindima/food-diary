using FoodDiary.Application.Runtime;
using FoodDiary.Application.Billing;
using FoodDiary.Application.Dietologist;
using FoodDiary.Modules.Fasting.Infrastructure;
using FoodDiary.Application.Favorites;
using FoodDiary.Application.Gamification;
using FoodDiary.Application.Identity;
using FoodDiary.Application.Images;
using FoodDiary.Application.Marketing;
using FoodDiary.Application.Meals;
using FoodDiary.Application.Notifications;
using FoodDiary.Application.Users;
using FoodDiary.Application.WeeklyGoals;
using FoodDiary.Infrastructure;
using FoodDiary.Integrations;
using FoodDiary.JobManager.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace FoodDiary.JobManager.Tests;

[ExcludeFromCodeCoverage]
public sealed class DependencyInjectionTests {
    [Fact]
    public void JobManagerProductionRegistrations_CanResolveRecurringJobs() {
        IConfiguration configuration = CreateConfiguration();
        ServiceCollection services = CreateProductionServices(configuration);

        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        using IServiceScope scope = provider.CreateScope();

        Assert.Multiple(
            () => Assert.NotNull(scope.ServiceProvider.GetRequiredService<ImageCleanupJob>()),
            () => Assert.NotNull(scope.ServiceProvider.GetRequiredService<BillingRenewalJob>()),
            () => Assert.NotNull(scope.ServiceProvider.GetRequiredService<FastingNotificationJob>()),
            () => Assert.NotNull(scope.ServiceProvider.GetRequiredService<AchievementEvaluationOutboxJob>()),
            () => Assert.NotNull(scope.ServiceProvider.GetRequiredService<ImageObjectDeletionOutboxJob>()),
            () => Assert.NotNull(scope.ServiceProvider.GetRequiredService<NotificationWebPushOutboxJob>()),
            () => Assert.NotNull(scope.ServiceProvider.GetRequiredService<NotificationCleanupJob>()),
            () => Assert.NotNull(scope.ServiceProvider.GetRequiredService<UserCleanupJob>()),
            () => Assert.NotNull(scope.ServiceProvider.GetRequiredService<UserLoginEventCleanupJob>()),
            () => Assert.NotNull(scope.ServiceProvider.GetRequiredService<MarketingAttributionCleanupJob>()),
            () => Assert.NotNull(scope.ServiceProvider.GetRequiredService<FastingTelemetryCleanupJob>()));
    }

    [Fact]
    public void JobManagerProductionOptions_AreValidAtStartup() {
        IConfiguration configuration = CreateConfiguration();
        ServiceCollection services = CreateProductionServices(configuration);

        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions {
            ValidateScopes = true,
        });

        Assert.Multiple(
            () => Assert.NotNull(provider.GetRequiredService<IOptions<ImageCleanupOptions>>().Value),
            () => Assert.NotNull(provider.GetRequiredService<IOptions<UserCleanupOptions>>().Value),
            () => Assert.NotNull(provider.GetRequiredService<IOptions<UserLoginEventCleanupOptions>>().Value),
            () => Assert.NotNull(provider.GetRequiredService<IOptions<MarketingAttributionCleanupOptions>>().Value),
            () => Assert.NotNull(provider.GetRequiredService<IOptions<FastingTelemetryCleanupOptions>>().Value),
            () => Assert.NotNull(provider.GetRequiredService<IOptions<NotificationCleanupOptions>>().Value),
            () => Assert.NotNull(provider.GetRequiredService<IOptions<BillingRenewalOptions>>().Value),
            () => Assert.NotNull(provider.GetRequiredService<IOptions<FastingNotificationOptions>>().Value),
            () => Assert.NotNull(provider.GetRequiredService<IOptions<ImageObjectDeletionOutboxOptions>>().Value),
            () => Assert.NotNull(provider.GetRequiredService<IOptions<NotificationWebPushOutboxOptions>>().Value));
    }

    [Fact]
    public void JobManagerProductionRegistrations_ConfigureMetricsExporter() {
        IConfiguration configuration = CreateConfiguration();
        ServiceCollection services = CreateProductionServices(configuration);

        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Multiple(
            () => Assert.NotNull(provider.GetRequiredService<MeterProvider>()),
            () => Assert.NotNull(provider.GetRequiredService<TracerProvider>()));
    }

    [Fact]
    public void AddJobManagerOpenTelemetry_WithoutEndpoint_ReturnsServicesWithoutTelemetryRegistration() {
        var services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder().Build();

        IServiceCollection result = services.AddJobManagerOpenTelemetry(configuration);

        Assert.Same(services, result);
        Assert.Empty(services);
    }

    [Fact]
    public void AddJobManagerOpenTelemetry_WithInvalidEndpoint_Throws() {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase) {
                ["OpenTelemetry:Otlp:Endpoint"] = "not-an-absolute-uri",
            })
            .Build();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddJobManagerOpenTelemetry(configuration));

        Assert.Contains("valid absolute URI", exception.Message, StringComparison.Ordinal);
    }

    private static ServiceCollection CreateProductionServices(IConfiguration configuration) {
        var services = new ServiceCollection();

        services.AddApplicationRuntime();
        services.AddUsersModule();
        services.AddBillingModule();
        services.AddDietologistModule();
        services.AddFastingModule();
        services.AddFavoritesModule();
        services.AddGamificationModule();
        services.AddIdentityModule();
        services.AddImagesModule();
        services.AddMarketingModule();
        services.AddMealsModule();
        services.AddNotificationsModule();
        services.AddWeeklyGoalsModule();
        services.AddInfrastructure(configuration);
        services.AddIntegrations(configuration);
        services.AddDataProtection();
        services.AddNotificationResources();
        services.AddJobManagerServices(configuration);
        services.AddJobManagerOpenTelemetry(configuration);

        return services;
    }

    private static IConfiguration CreateConfiguration() {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase) {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=fooddiary_test;Username=test;Password=test",
            ["OpenTelemetry:Otlp:Endpoint"] = "http://localhost:4317",
            ["Jwt:SecretKey"] = "test-secret-key-for-di-validation-32",
            ["Jwt:Issuer"] = "FoodDiary.Tests",
            ["Jwt:Audience"] = "FoodDiary.Tests",
            ["Jwt:ExpirationMinutes"] = "60",
            ["Jwt:RefreshTokenExpirationDays"] = "30",
            ["Jwt:RememberMeRefreshTokenExpirationDays"] = "90",
            ["Email:FrontendBaseUrl"] = "https://example.test",
            ["Email:VerificationPath"] = "/verify-email",
            ["Email:PasswordResetPath"] = "/reset-password",
            ["S3:AccessKeyId"] = "test-access-key",
            ["S3:SecretAccessKey"] = "test-secret-key",
            ["S3:Region"] = "us-east-1",
            ["S3:Bucket"] = "fooddiary-test",
            ["S3:StagingBucket"] = "fooddiary-test-staging",
            ["S3:ServiceUrl"] = "http://localhost:9000",
            ["S3:AllowInsecureHttp"] = "true",
            ["S3:AllowPublicImageAccess"] = "true",
            ["S3:MaxUploadSizeBytes"] = "1048576",
            ["Billing:Provider"] = "Stripe",
            ["WebPush:Enabled"] = "false",
            ["ImageCleanup:OlderThanHours"] = "12",
            ["ImageCleanup:BatchSize"] = "10",
            ["ImageCleanup:Cron"] = "0 * * * *",
            ["UserCleanup:RetentionDays"] = "30",
            ["UserCleanup:BatchSize"] = "25",
            ["UserCleanup:Cron"] = "0 3 * * *",
            ["UserLoginEventCleanup:Enabled"] = "true",
            ["UserLoginEventCleanup:RetentionDays"] = "180",
            ["UserLoginEventCleanup:BatchSize"] = "500",
            ["UserLoginEventCleanup:Cron"] = "0 3 * * *",
            ["MarketingAttributionCleanup:Enabled"] = "true",
            ["MarketingAttributionCleanup:RetentionDays"] = "365",
            ["MarketingAttributionCleanup:BatchSize"] = "500",
            ["MarketingAttributionCleanup:Cron"] = "30 3 * * *",
            ["FastingTelemetryCleanup:Enabled"] = "true",
            ["FastingTelemetryCleanup:RetentionDays"] = "90",
            ["FastingTelemetryCleanup:BatchSize"] = "500",
            ["FastingTelemetryCleanup:Cron"] = "45 3 * * *",
            ["NotificationCleanup:TransientTypes:0"] = "FastingCheckInReminder",
            ["NotificationCleanup:TransientReadRetentionDays"] = "14",
            ["NotificationCleanup:TransientUnreadRetentionDays"] = "30",
            ["NotificationCleanup:StandardReadRetentionDays"] = "60",
            ["NotificationCleanup:StandardUnreadRetentionDays"] = "90",
            ["NotificationCleanup:BatchSize"] = "100",
            ["NotificationCleanup:Cron"] = "0 4 * * *",
            ["BillingRenewal:Enabled"] = "true",
            ["BillingRenewal:Provider"] = "YooKassa",
            ["BillingRenewal:BatchSize"] = "50",
            ["BillingRenewal:Cron"] = "15 * * * *",
            ["FastingNotifications:Enabled"] = "true",
            ["FastingNotifications:Cron"] = "* * * * *",
            ["ImageObjectDeletionOutbox:Enabled"] = "true",
            ["ImageObjectDeletionOutbox:BatchSize"] = "25",
            ["ImageObjectDeletionOutbox:Cron"] = "* * * * *",
            ["NotificationWebPushOutbox:Enabled"] = "true",
            ["NotificationWebPushOutbox:BatchSize"] = "50",
            ["NotificationWebPushOutbox:Cron"] = "* * * * *",
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
