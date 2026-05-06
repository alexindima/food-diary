using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Email.Common;
using FoodDiary.Application.Abstractions.Export.Common;
using FoodDiary.Infrastructure.Options;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Services;
using FoodDiary.Integrations;
using FoodDiary.MailRelay.Client.Options;
using FoodDiary.Mediator;
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

        var ex = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<EmailOptions>>().Value);
        Assert.Contains("FrontendBaseUrl", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddInfrastructure_WithInvalidAllowedEmailBaseUrl_FailsOptionsValidation() {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(new Dictionary<string, string?> {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=food_diary;Username=test;Password=test",
            ["Jwt:SecretKey"] = "super-secret-key-for-tests-only-123456789",
            ["Jwt:Issuer"] = "FoodDiary",
            ["Jwt:Audience"] = "FoodDiaryClients",
            ["Jwt:ExpirationMinutes"] = "60",
            ["Jwt:RefreshTokenExpirationDays"] = "7",
            ["Email:FrontendBaseUrl"] = "https://fooddiary.club",
            ["Email:AllowedFrontendBaseUrls:0"] = "not-a-url"
        });

        services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();

        var ex = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<EmailOptions>>().Value);
        Assert.Contains("AllowedFrontendBaseUrls", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddIntegrations_WithInvalidMailRelayClientBaseUrl_FailsOptionsValidation() {
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

        services.AddIntegrations(configuration);
        using var provider = services.BuildServiceProvider();

        var ex = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<MailRelayClientOptions>>().Value);
        Assert.Contains("base URL", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddIntegrations_WithInvalidS3ServiceUrl_FailsOptionsValidation() {
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

        services.AddIntegrations(configuration);
        using var provider = services.BuildServiceProvider();

        var ex = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<FoodDiary.Integrations.Options.S3Options>>().Value);
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
    public void AddInfrastructure_CanResolveDiaryPdfGeneratorTypedClient() {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(new Dictionary<string, string?> {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=food_diary;Username=test;Password=test",
            ["Jwt:SecretKey"] = "super-secret-key-for-tests-only-123456789",
            ["Jwt:Issuer"] = "FoodDiary",
            ["Jwt:Audience"] = "FoodDiaryClients",
            ["Jwt:ExpirationMinutes"] = "60",
            ["Jwt:RefreshTokenExpirationDays"] = "7"
        });

        services.AddSingleton<IDiaryPdfReportTextProvider, TestDiaryPdfReportTextProvider>();
        services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();

        var generator = provider.GetRequiredService<IDiaryPdfGenerator>();

        Assert.NotNull(generator);
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
