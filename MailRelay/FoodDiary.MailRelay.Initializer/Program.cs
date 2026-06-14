using System.Diagnostics.CodeAnalysis;
using FoodDiary.MailRelay.Application.Abstractions;
using FoodDiary.MailRelay.Initializer;
using FoodDiary.MailRelay.Infrastructure.Options;
using FoodDiary.MailRelay.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

var command = InitializerCommand.Parse(args);
if (command is null) {
    PrintUsage();
    return 1;
}

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

string webApiSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "MailRelay", "FoodDiary.MailRelay.WebApi");
if (Directory.Exists(webApiSettingsPath)) {
    builder.Configuration
        .AddJsonFile(Path.Combine(webApiSettingsPath, "appsettings.json"), optional: true, reloadOnChange: false)
        .AddJsonFile(
            Path.Combine(webApiSettingsPath, $"appsettings.{builder.Environment.EnvironmentName}.json"),
            optional: true,
            reloadOnChange: false);
}

if (builder.Environment.IsDevelopment()) {
    builder.Configuration.AddUserSecrets<Program>();
}

builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddEnvironmentVariables("FOODDIARY_");
builder.Configuration.AddEnvironmentVariables("MAILRELAY_");

if (!string.IsNullOrWhiteSpace(command.ConnectionString)) {
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
        ["ConnectionStrings:DefaultConnection"] = command.ConnectionString,
    });
}

string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString)) {
    Console.Error.WriteLine(
        "MailRelay initializer failed: DefaultConnection is not configured. Pass --connection-string, set ConnectionStrings__DefaultConnection, set FOODDIARY_ConnectionStrings__DefaultConnection, set MAILRELAY_ConnectionStrings__DefaultConnection, or provide appsettings in MailRelay/FoodDiary.MailRelay.WebApi.");
    return 1;
}

builder.Services.AddOptions<MailRelayQueueOptions>()
    .Bind(builder.Configuration.GetSection(MailRelayQueueOptions.SectionName))
    .Validate(MailRelayQueueOptions.HasValidConfiguration,
        "MailRelayQueue configuration requires positive poll interval, batch size, retry delays, and lock timeout.")
    .ValidateOnStart();
builder.Services.AddSingleton(_ => new NpgsqlDataSourceBuilder(connectionString).Build());
builder.Services.AddSingleton<MailRelayQueueStore>();
builder.Services.AddSingleton<IMailRelaySchemaInitializer>(sp => sp.GetRequiredService<MailRelayQueueStore>());

using IHost host = builder.Build();
AsyncServiceScope scope = host.Services.CreateAsyncScope();
await using (scope.ConfigureAwait(false)) {
    NpgsqlDataSource dataSource = scope.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
    IMailRelaySchemaInitializer schemaInitializer = scope.ServiceProvider.GetRequiredService<IMailRelaySchemaInitializer>();

    try {
        await ExecuteAsync(command, dataSource, schemaInitializer, CancellationToken.None).ConfigureAwait(false);
        return 0;
    } catch (Exception exception) {
        Console.Error.WriteLine($"MailRelay initializer failed: {exception}");
        return 1;
    }
}

static async Task ExecuteAsync(
    InitializerCommand command,
    NpgsqlDataSource dataSource,
    IMailRelaySchemaInitializer schemaInitializer,
    CancellationToken cancellationToken) {
    switch (command.Name) {
        case "status":
            await PrintStatusAsync(dataSource, cancellationToken).ConfigureAwait(false);
            break;
        case "update":
            Console.WriteLine("Updating MailRelay schema...");
            await schemaInitializer.EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);
            Console.WriteLine("MailRelay schema update completed.");
            break;
        default:
            throw new InvalidOperationException($"Unknown command '{command.Name}'.");
    }
}

static async Task PrintStatusAsync(NpgsqlDataSource dataSource, CancellationToken cancellationToken) {
    NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
    await using (connection.ConfigureAwait(false)) {
        string[] requiredTables = [
        "mailrelay_outbound_emails",
        "mailrelay_outbox_messages",
        "mailrelay_inbox_messages",
        "mailrelay_suppressions",
        "mailrelay_delivery_events",
    ];
        var existingTables = new HashSet<string>(StringComparer.Ordinal);

        const string sql = """
                       select table_name
                       from information_schema.tables
                       where table_schema = 'public'
                         and table_name = any(@tableNames);
                       """;

        var command = new NpgsqlCommand(sql, connection);
        await using (command.ConfigureAwait(false)) {
            command.Parameters.AddWithValue("tableNames", requiredTables);
            NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            await using (reader.ConfigureAwait(false)) {
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) {
                    existingTables.Add(reader.GetString(0));
                }

                Console.WriteLine("Can connect:       True");
                Console.WriteLine($"Required tables:   {requiredTables.Length}");
                Console.WriteLine($"Existing tables:   {existingTables.Count}");

                foreach (string? table in requiredTables) {
                    string state = existingTables.Contains(table) ? "present" : "missing";
                    Console.WriteLine($"{state,-8} {table}");
                }
            }
        }
    }
}

static void PrintUsage() {
    Console.WriteLine("""
Usage:
  dotnet run --project MailRelay/FoodDiary.MailRelay.Initializer -- <command> [--connection-string "<value>"]

Commands:
  status                  Show MailRelay schema status
  update                  Create or update MailRelay schema

Examples:
  dotnet run --project MailRelay/FoodDiary.MailRelay.Initializer -- status
  dotnet run --project MailRelay/FoodDiary.MailRelay.Initializer -- update
  dotnet run --project MailRelay/FoodDiary.MailRelay.Initializer -- update --connection-string "Host=..."
""");
}

[ExcludeFromCodeCoverage]
public partial class Program;
