using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Options;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Services;
using FoodDiary.MailRelay.Client.Options;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FoodDiary.Infrastructure.Tests;

public sealed class DependencyInjectionTests {
    [Fact]
    public void AddInfrastructure_WithInvalidEmailBaseUrl_FailsOptionsValidation() {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(new Dictionary<string, string?> {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=food_diary;Username=test;Password=test",
            ["Jwt:SecretKey"] = "super-secret-key-for-tests-only-123456789",
            ["Jwt:Issuer"] = "FoodDiary",
            ["Jwt:Audience"] = "FoodDiaryClients",
            ["Jwt:ExpirationMinutes"] = "60",
            ["Jwt:RefreshTokenExpirationDays"] = "7",
            ["Email:FrontendBaseUrl"] = "not-a-url"
        });

        services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();

        var ex = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<FoodDiary.Infrastructure.Options.EmailOptions>>().Value);
        Assert.Contains("FrontendBaseUrl", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddInfrastructure_WithInvalidMailRelayClientBaseUrl_FailsOptionsValidation() {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(new Dictionary<string, string?> {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=food_diary;Username=test;Password=test",
            ["Jwt:SecretKey"] = "super-secret-key-for-tests-only-123456789",
            ["Jwt:Issuer"] = "FoodDiary",
            ["Jwt:Audience"] = "FoodDiaryClients",
            ["Jwt:ExpirationMinutes"] = "60",
            ["Jwt:RefreshTokenExpirationDays"] = "7",
            ["MailRelayClient:BaseUrl"] = "not-a-url"
        });

        services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();

        var ex = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<MailRelayClientOptions>>().Value);
        Assert.Contains("base URL", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddInfrastructure_WithInvalidS3ServiceUrl_FailsOptionsValidation() {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(new Dictionary<string, string?> {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=food_diary;Username=test;Password=test",
            ["Jwt:SecretKey"] = "super-secret-key-for-tests-only-123456789",
            ["Jwt:Issuer"] = "FoodDiary",
            ["Jwt:Audience"] = "FoodDiaryClients",
            ["Jwt:ExpirationMinutes"] = "60",
            ["Jwt:RefreshTokenExpirationDays"] = "7",
            ["S3:ServiceUrl"] = "invalid-url"
        });

        services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();

        var ex = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<FoodDiary.Infrastructure.Options.S3Options>>().Value);
        Assert.Contains("ServiceUrl", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddInfrastructure_RegistersDatabaseCommandTelemetryInterceptor() {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(new Dictionary<string, string?> {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=food_diary;Username=test;Password=test",
            ["Jwt:SecretKey"] = "super-secret-key-for-tests-only-123456789",
            ["Jwt:Issuer"] = "FoodDiary",
            ["Jwt:Audience"] = "FoodDiaryClients",
            ["Jwt:ExpirationMinutes"] = "60",
            ["Jwt:RefreshTokenExpirationDays"] = "7"
        });

        services.AddInfrastructure(configuration);

        var interceptorDescriptor = Assert.Single(
            services,
            static descriptor => descriptor.ServiceType == typeof(DatabaseCommandTelemetryInterceptor));
        Assert.Equal(ServiceLifetime.Singleton, interceptorDescriptor.Lifetime);
    }

    [Fact]
    public void AddInfrastructure_WithInvalidDatabaseRetryCount_FailsOptionsValidation() {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(new Dictionary<string, string?> {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=food_diary;Username=test;Password=test",
            ["Jwt:SecretKey"] = "super-secret-key-for-tests-only-123456789",
            ["Jwt:Issuer"] = "FoodDiary",
            ["Jwt:Audience"] = "FoodDiaryClients",
            ["Jwt:ExpirationMinutes"] = "60",
            ["Jwt:RefreshTokenExpirationDays"] = "7",
            ["Database:EnableRetries"] = "true",
            ["Database:MaxRetryCount"] = "0"
        });

        services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();

        var ex = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<DatabaseOptions>>().Value);
        Assert.Contains("MaxRetryCount", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddInfrastructure_WithRetriesEnabled_ConfiguresRetryingExecutionStrategy() {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(new Dictionary<string, string?> {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=food_diary;Username=test;Password=test",
            ["Jwt:SecretKey"] = "super-secret-key-for-tests-only-123456789",
            ["Jwt:Issuer"] = "FoodDiary",
            ["Jwt:Audience"] = "FoodDiaryClients",
            ["Jwt:ExpirationMinutes"] = "60",
            ["Jwt:RefreshTokenExpirationDays"] = "7",
            ["Database:EnableRetries"] = "true",
            ["Database:MaxRetryCount"] = "4",
            ["Database:MaxRetryDelaySeconds"] = "7"
        });

        services.AddSingleton<IPublisher>(new NullPublisher());
        services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();

        var strategy = context.Database.CreateExecutionStrategy();

        Assert.Contains("Retry", strategy.GetType().Name, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FoodDiaryDbContext_Model_ConfiguresCascadeDeleteForAllUserOwnedEntities() {
        var options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
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
        var entityType = context.Model.FindEntityType(clrType);
        if (entityType is null) {
            return $"{clrType.FullName}: not mapped in FoodDiaryDbContext.";
        }

        var foreignKey = entityType
            .GetForeignKeys()
            .SingleOrDefault(fk => fk.Properties.Any(property => property.Name == "UserId"));

        if (foreignKey is null) {
            return $"{clrType.FullName}: missing FK for UserId.";
        }

        return foreignKey.DeleteBehavior != DeleteBehavior.Cascade
            ? $"{clrType.FullName}: expected DeleteBehavior.Cascade, got {foreignKey.DeleteBehavior}."
            : null;
    }

    private sealed class NullPublisher : IPublisher {
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => Task.CompletedTask;
    }
}
