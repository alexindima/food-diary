using FoodDiary.BugTriage.Application.Abstractions;
using FoodDiary.BugTriage.Application.Reports;
using FoodDiary.BugTriage.Infrastructure.Mail;
using FoodDiary.BugTriage.Infrastructure.Options;
using FoodDiary.BugTriage.Infrastructure.Persistence;
using FoodDiary.BugTriage.Infrastructure.Workers;
using FoodDiary.MailInbox.Client.Export;
using FoodDiary.MailInbox.Client.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;

namespace FoodDiary.BugTriage.Infrastructure.Extensions;

public static class BugTriageInfrastructureExtensions {
    public static IServiceCollection AddBugTriageInfrastructure(this IServiceCollection services, IConfiguration configuration) {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(_ => {
            string connectionString = configuration.GetConnectionString("BugTriage")
                ?? throw new InvalidOperationException("ConnectionStrings:BugTriage is required.");
            var options = new NpgsqlConnectionStringBuilder(connectionString);
            if (!string.Equals(options.Database, "fooddiary_bugtriage", StringComparison.Ordinal)) {
                throw new InvalidOperationException("BugTriage requires its separate fooddiary_bugtriage database.");
            }
            return NpgsqlDataSource.Create(connectionString);
        });
        services.AddSingleton<IBugReportStore, NpgsqlBugReportStore>();
        services.AddSingleton<IBugMailSource, MailInboxBugSource>();
        services.AddSingleton<ImportBugReports>();
        services.AddOptions<BugTriageOptions>().Bind(configuration.GetSection("BugTriage"))
            .Validate(static o => System.Net.Mail.MailAddress.TryCreate(o.Recipient, out _) &&
                o.PollInterval >= TimeSpan.FromSeconds(10) && o.PollInterval <= TimeSpan.FromDays(1) &&
                o.ImportTimeout >= TimeSpan.FromSeconds(10) && o.ImportTimeout <= TimeSpan.FromHours(1) &&
                o.ContentRetention >= TimeSpan.FromDays(1) && o.ContentRetention <= TimeSpan.FromDays(90) &&
                o.MaxConcurrentReports is >= 1 and <= 10 && o.MaxImportsPerPoll is >= 1 and <= 1000, "Invalid BugTriage settings.")
            .ValidateOnStart();
        services.AddOptions<MailInboxClientOptions>().Bind(configuration.GetSection(MailInboxClientOptions.SectionName))
            .Validate(MailInboxClientOptions.HasValidBaseUrl, "A trusted HTTPS MailInbox URL is required.")
            .Validate(static o => o.MetadataApiKey.Length is >= 32 and <= 256 && o.ContentApiKey.Length is >= 32 and <= 256 &&
                !string.Equals(o.MetadataApiKey, o.ContentApiKey, StringComparison.Ordinal), "Distinct MailInbox read keys are required.")
            .ValidateOnStart();
        services.AddHttpClient<IMailInboxExportClient, MailInboxExportClient>((sp, client) => {
            MailInboxClientOptions options = sp.GetRequiredService<IOptions<MailInboxClientOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
            client.MaxResponseContentBufferSize = 10 * 1024 * 1024;
        }).ConfigurePrimaryHttpMessageHandler(static () => new HttpClientHandler { AllowAutoRedirect = false });
        services.AddHostedService<BugMailImportWorker>();
        return services;
    }
}
