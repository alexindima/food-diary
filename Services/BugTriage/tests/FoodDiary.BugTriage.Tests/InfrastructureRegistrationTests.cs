using FoodDiary.BugTriage.Application.Abstractions;
using FoodDiary.BugTriage.Application.Reports;
using FoodDiary.BugTriage.Infrastructure.Extensions;
using FoodDiary.MailInbox.Client.Export;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace FoodDiary.BugTriage.Tests;

[ExcludeFromCodeCoverage]
public sealed class InfrastructureRegistrationTests {
    [Theory]
    [InlineData(null)]
    [InlineData("Host=localhost;Database=fooddiary")]
    public void DataSource_RejectsMissingOrSharedDatabase(string? connectionString) {
        IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["ConnectionStrings:BugTriage"] = connectionString,
        }).Build();
        var services = new ServiceCollection();
        services.AddBugTriageInfrastructure(config);
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<NpgsqlDataSource>());
    }

    [Fact]
    public void Registrations_ResolveDedicatedDatabaseAndImportPipelineWithoutConnecting() {
        IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["ConnectionStrings:BugTriage"] = "Host=localhost;Database=fooddiary_bugtriage;Username=test;Password=test",
            ["MailInboxClient:BaseUrl"] = "https://inbox.example.test",
            ["MailInboxClient:MetadataApiKey"] = new string('m', 32),
            ["MailInboxClient:ContentApiKey"] = new string('c', 32),
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBugTriageInfrastructure(config);
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Multiple(
            () => Assert.NotNull(provider.GetRequiredService<ImportBugReports>()),
            () => Assert.NotNull(provider.GetRequiredService<IMailInboxExportClient>()),
            () => Assert.Same(provider.GetRequiredService<IBugReportStore>(), provider.GetRequiredService<IBugReportStore>()),
            () => Assert.Equal("fooddiary_bugtriage", new NpgsqlConnectionStringBuilder(provider.GetRequiredService<NpgsqlDataSource>().ConnectionString).Database));
    }
}
