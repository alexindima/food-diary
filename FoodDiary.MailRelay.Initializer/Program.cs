using FoodDiary.MailRelay.Application.Abstractions;
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

var builder = Host.CreateApplicationBuilder(args);

var webApiSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "FoodDiary.MailRelay.WebApi");
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
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> {
        ["ConnectionStrings:DefaultConnection"] = command.ConnectionString
    });
}

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString)) {
    Console.Error.WriteLine(
        "MailRelay initializer failed: DefaultConnection is not configured. Pass --connection-string, set ConnectionStrings__DefaultConnection, set FOODDIARY_ConnectionStrings__DefaultConnection, set MAILRELAY_ConnectionStrings__DefaultConnection, or provide appsettings in FoodDiary.MailRelay.WebApi.");
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

using var host = builder.Build();
using var scope = host.Services.CreateScope();
var dataSource = scope.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
var schemaInitializer = scope.ServiceProvider.GetRequiredService<IMailRelaySchemaInitializer>();

try {
    await ExecuteAsync(command, dataSource, schemaInitializer, CancellationToken.None);
    return 0;
} catch (Exception exception) {
    Console.Error.WriteLine($"MailRelay initializer failed: {exception}");
    return 1;
}

static async Task ExecuteAsync(
    InitializerCommand command,
    NpgsqlDataSource dataSource,
    IMailRelaySchemaInitializer schemaInitializer,
    CancellationToken cancellationToken) {
    switch (command.Name) {
        case "status":
            await PrintStatusAsync(dataSource, cancellationToken);
            break;
        case "update":
            Console.WriteLine("Updating MailRelay schema...");
            await schemaInitializer.EnsureSchemaAsync(cancellationToken);
            Console.WriteLine("MailRelay schema update completed.");
            break;
        default:
            throw new InvalidOperationException($"Unknown command '{command.Name}'.");
    }
}

static async Task PrintStatusAsync(NpgsqlDataSource dataSource, CancellationToken cancellationToken) {
    await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
    var requiredTables = new[] {
        "mailrelay_outbound_emails",
        "mailrelay_outbox_messages",
        "mailrelay_inbox_messages",
        "mailrelay_suppressions",
        "mailrelay_delivery_events"
    };
    var existingTables = new HashSet<string>(StringComparer.Ordinal);

    const string sql = """
                       select table_name
                       from information_schema.tables
                       where table_schema = 'public'
                         and table_name = any(@tableNames);
                       """;

    await using var command = new NpgsqlCommand(sql, connection);
    command.Parameters.AddWithValue("tableNames", requiredTables);
    await using var reader = await command.ExecuteReaderAsync(cancellationToken);
    while (await reader.ReadAsync(cancellationToken)) {
        existingTables.Add(reader.GetString(0));
    }

    Console.WriteLine("Can connect:       True");
    Console.WriteLine($"Required tables:   {requiredTables.Length}");
    Console.WriteLine($"Existing tables:   {existingTables.Count}");

    foreach (var table in requiredTables) {
        var state = existingTables.Contains(table) ? "present" : "missing";
        Console.WriteLine($"{state,-8} {table}");
    }
}

static void PrintUsage() {
    Console.WriteLine("""
Usage:
  dotnet run --project FoodDiary.MailRelay.Initializer -- <command> [--connection-string "<value>"]

Commands:
  status                  Show MailRelay schema status
  update                  Create or update MailRelay schema

Examples:
  dotnet run --project FoodDiary.MailRelay.Initializer -- status
  dotnet run --project FoodDiary.MailRelay.Initializer -- update
  dotnet run --project FoodDiary.MailRelay.Initializer -- update --connection-string "Host=..."
""");
}

internal sealed record InitializerCommand(string Name, string? ConnectionString) {
    public static InitializerCommand? Parse(string[] args) {
        if (args.Length == 0) {
            return null;
        }

        string? name = null;
        string? connectionString = null;

        for (var index = 0; index < args.Length; index++) {
            var argument = args[index];

            if (argument is "--connection-string" or "-c") {
                index++;
                if (index >= args.Length) {
                    throw new InvalidOperationException("Missing value for --connection-string.");
                }

                connectionString = args[index];
                continue;
            }

            if (name is null) {
                name = argument;
                continue;
            }

            throw new InvalidOperationException($"Unexpected argument '{argument}'.");
        }

        return name is null ? null : new InitializerCommand(name, connectionString);
    }
}
