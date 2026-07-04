using FoodDiary.Application;
using FoodDiary.Infrastructure;
using FoodDiary.Integrations;
using FoodDiary.JobManager.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FoodDiary.JobManager.Tests;

[ExcludeFromCodeCoverage]
public sealed class DependencyInjectionTests {
    [Fact]
    public void JobManagerProductionRegistrations_CanResolveRecurringJobs() {
        IConfiguration configuration = CreateConfiguration();
        ServiceCollection services = CreateProductionServices(configuration);

        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions {
            ValidateScopes = true,
        });
        using IServiceScope scope = provider.CreateScope();

        Assert.Multiple(
            () => Assert.NotNull(scope.ServiceProvider.GetRequiredService<ImageCleanupJob>()),
            () => Assert.NotNull(scope.ServiceProvider.GetRequiredService<BillingRenewalJob>()),
            () => Assert.NotNull(scope.ServiceProvider.GetRequiredService<FastingNotificationJob>()),
            () => Assert.NotNull(scope.ServiceProvider.GetRequiredService<ImageObjectDeletionOutboxJob>()),
            () => Assert.NotNull(scope.ServiceProvider.GetRequiredService<NotificationWebPushOutboxJob>()),
            () => Assert.NotNull(scope.ServiceProvider.GetRequiredService<NotificationCleanupJob>()),
            () => Assert.NotNull(scope.ServiceProvider.GetRequiredService<UserCleanupJob>()),
            () => Assert.NotNull(scope.ServiceProvider.GetRequiredService<UserLoginEventCleanupJob>()));
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
            () => Assert.NotNull(provider.GetRequiredService<IOptions<NotificationCleanupOptions>>().Value),
            () => Assert.NotNull(provider.GetRequiredService<IOptions<BillingRenewalOptions>>().Value),
            () => Assert.NotNull(provider.GetRequiredService<IOptions<FastingNotificationOptions>>().Value),
            () => Assert.NotNull(provider.GetRequiredService<IOptions<ImageObjectDeletionOutboxOptions>>().Value),
            () => Assert.NotNull(provider.GetRequiredService<IOptions<NotificationWebPushOutboxOptions>>().Value));
    }

    private static ServiceCollection CreateProductionServices(IConfiguration configuration) {
        var services = new ServiceCollection();

        services.AddApplication();
        services.AddInfrastructure(configuration);
        services.AddIntegrations(configuration);
        services.AddNotificationResources();
        services.AddJobManagerServices(configuration);

        return services;
    }

    private static IConfiguration CreateConfiguration() {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase) {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=fooddiary_test;Username=test;Password=test",
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
            ["S3:ServiceUrl"] = "http://localhost:9000",
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
