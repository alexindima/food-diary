using System.Diagnostics.CodeAnalysis;
using FoodDiary.MailInbox.Application.Abstractions;
using FoodDiary.MailInbox.Initializer;
using FoodDiary.MailInbox.Infrastructure.Services;
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

string webApiSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "FoodDiary.MailInbox.WebApi");
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
builder.Configuration.AddEnvironmentVariables("MAILINBOX_");

if (!string.IsNullOrWhiteSpace(command.ConnectionString)) {
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
        ["ConnectionStrings:DefaultConnection"] = command.ConnectionString
    });
}

string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString)) {
    Console.Error.WriteLine(
        "MailInbox initializer failed: DefaultConnection is not configured. Pass --connection-string, set ConnectionStrings__DefaultConnection, set FOODDIARY_ConnectionStrings__DefaultConnection, set MAILINBOX_ConnectionStrings__DefaultConnection, or provide appsettings in FoodDiary.MailInbox.WebApi.");
    return 1;
}

builder.Services.AddSingleton(_ => new NpgsqlDataSourceBuilder(connectionString).Build());
builder.Services.AddSingleton<DmarcReportParser>();
builder.Services.AddSingleton<NpgsqlInboundMailStore>();
builder.Services.AddSingleton<IMailInboxSchemaInitializer>(sp => sp.GetRequiredService<NpgsqlInboundMailStore>());

using IHost host = builder.Build();
using IServiceScope scope = host.Services.CreateScope();
NpgsqlDataSource dataSource = scope.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
IMailInboxSchemaInitializer schemaInitializer = scope.ServiceProvider.GetRequiredService<IMailInboxSchemaInitializer>();

try {
    await ExecuteAsync(command, dataSource, schemaInitializer).ConfigureAwait(false);
    return 0;
} catch (Exception exception) {
    Console.Error.WriteLine($"MailInbox initializer failed: {exception}");
    return 1;
}

static async Task ExecuteAsync(
    InitializerCommand command,
    NpgsqlDataSource dataSource,
    IMailInboxSchemaInitializer schemaInitializer) {
    switch (command.Name) {
        case "status":
            await PrintStatusAsync(dataSource).ConfigureAwait(false);
            break;
        case "update":
            Console.WriteLine("Updating MailInbox schema...");
            await schemaInitializer.EnsureSchemaAsync(CancellationToken.None).ConfigureAwait(false);
            Console.WriteLine("MailInbox schema update completed.");
            break;
        default:
            throw new InvalidOperationException($"Unknown command '{command.Name}'.");
    }
}

static async Task PrintStatusAsync(NpgsqlDataSource dataSource) {
    NpgsqlConnection connection = await dataSource.OpenConnectionAsync().ConfigureAwait(false);
    await using (connection.ConfigureAwait(false)) {
        string[] requiredTables = new[] {
        "mailinbox_messages"
    };
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
            using NpgsqlDataReader reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false)) {
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

static void PrintUsage() {
    Console.WriteLine("""
Usage:
  dotnet run --project FoodDiary.MailInbox.Initializer -- <command> [--connection-string "<value>"]

Commands:
  status                  Show MailInbox schema status
  update                  Create or update MailInbox schema

Examples:
  dotnet run --project FoodDiary.MailInbox.Initializer -- status
  dotnet run --project FoodDiary.MailInbox.Initializer -- update
  dotnet run --project FoodDiary.MailInbox.Initializer -- update --connection-string "Host=..."
""");
}

[ExcludeFromCodeCoverage]
public partial class Program;

namespace FoodDiary.MailInbox.Initializer {
    internal sealed record InitializerCommand(string Name, string? ConnectionString) {
        public static InitializerCommand? Parse(string[] args) {
            if (args.Length == 0) {
                return null;
            }

            string? name = null;
            string? connectionString = null;

            for (int index = 0; index < args.Length; index++) {
                string argument = args[index];

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
}
