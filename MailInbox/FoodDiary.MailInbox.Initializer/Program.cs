using System.Diagnostics.CodeAnalysis;
using FoodDiary.MailInbox.Application.Abstractions;
using FoodDiary.MailInbox.Initializer;
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

string webApiSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "MailInbox", "FoodDiary.MailInbox.WebApi");
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
        ["ConnectionStrings:DefaultConnection"] = command.ConnectionString,
    });
}

string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString)) {
    Console.Error.WriteLine(
        "MailInbox initializer failed: DefaultConnection is not configured. Pass --connection-string, set ConnectionStrings__DefaultConnection, set FOODDIARY_ConnectionStrings__DefaultConnection, set MAILINBOX_ConnectionStrings__DefaultConnection, or provide appsettings in MailInbox/FoodDiary.MailInbox.WebApi.");
    return 1;
}

builder.Services.AddMailInboxInitializerServices(connectionString);

using IHost host = builder.Build();
AsyncServiceScope scope = host.Services.CreateAsyncScope();
await using (scope.ConfigureAwait(false)) {
    NpgsqlDataSource dataSource = scope.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
    IMailInboxSchemaInitializer schemaInitializer = scope.ServiceProvider.GetRequiredService<IMailInboxSchemaInitializer>();

    try {
        await ExecuteAsync(command, dataSource, schemaInitializer).ConfigureAwait(false);
        return 0;
    } catch (Exception exception) {
        Console.Error.WriteLine($"MailInbox initializer failed: {exception}");
        return 1;
    }
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
        string[] requiredTables = [
            "mailinbox_messages",
            "mailinbox_schema_migrations",
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
            NpgsqlDataReader reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            await using (reader.ConfigureAwait(false)) {
                while (await reader.ReadAsync().ConfigureAwait(false)) {
                    existingTables.Add(reader.GetString(0));
                }

                Console.WriteLine("Can connect:       True");
                Console.WriteLine($"Required tables:   {requiredTables.Length}");
                Console.WriteLine($"Existing tables:   {existingTables.Count}");

                foreach (string table in requiredTables) {
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
  dotnet run --project MailInbox/FoodDiary.MailInbox.Initializer -- <command> [--connection-string "<value>"]

Commands:
  status                  Show MailInbox schema status
  update                  Create or update MailInbox schema

Examples:
  dotnet run --project MailInbox/FoodDiary.MailInbox.Initializer -- status
  dotnet run --project MailInbox/FoodDiary.MailInbox.Initializer -- update
  dotnet run --project MailInbox/FoodDiary.MailInbox.Initializer -- update --connection-string "Host=..."
""");
}

[ExcludeFromCodeCoverage]
public partial class Program;
