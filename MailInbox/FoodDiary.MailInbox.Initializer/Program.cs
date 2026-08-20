using System.Diagnostics.CodeAnalysis;
using FoodDiary.MailInbox.Application.Abstractions;
using FoodDiary.MailInbox.Initializer;
using FoodDiary.MailInbox.Initializer.Options;
using FoodDiary.MailInbox.Infrastructure.Options;
using FoodDiary.MailInbox.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

InitializerCommand? command;
try {
    command = InitializerCommand.Parse(args);
} catch (InvalidOperationException) {
    Console.Error.WriteLine("MailInbox initializer failed: invalid command arguments.");
    PrintUsage();
    return 1;
}

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

string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString)) {
    Console.Error.WriteLine(
        "MailInbox initializer failed: DefaultConnection is not configured. Set ConnectionStrings__DefaultConnection, set FOODDIARY_ConnectionStrings__DefaultConnection, set MAILINBOX_ConnectionStrings__DefaultConnection, use development user secrets, or provide appsettings in MailInbox/FoodDiary.MailInbox.WebApi.");
    return 1;
}

if (!MailInboxDatabaseConfiguration.TargetsRequiredDatabase(connectionString)) {
    Console.Error.WriteLine(
        $"MailInbox initializer failed: DefaultConnection must target the dedicated '{MailInboxDatabaseConfiguration.RequiredDatabaseName}' database.");
    return 1;
}

if (builder.Environment.IsProduction() &&
    !MailInboxDatabaseConfiguration.UsesAuthenticatedTls(connectionString)) {
    Console.Error.WriteLine(
        "MailInbox initializer failed: Production DefaultConnection must use SSL Mode=VerifyFull.");
    return 1;
}

builder.Services.AddMailInboxInitializerServices(connectionString, builder.Configuration);

using IHost host = builder.Build();
try {
    await host.StartAsync().ConfigureAwait(false);
    AsyncServiceScope scope = host.Services.CreateAsyncScope();
    await using (scope.ConfigureAwait(false)) {
        IMailInboxReadinessChecker readinessChecker = scope.ServiceProvider.GetRequiredService<IMailInboxReadinessChecker>();
        IMailInboxSchemaInitializer schemaInitializer = scope.ServiceProvider.GetRequiredService<IMailInboxSchemaInitializer>();
        NpgsqlMailInboxRuntimeRoleProvisioner runtimeRoleProvisioner =
            scope.ServiceProvider.GetRequiredService<NpgsqlMailInboxRuntimeRoleProvisioner>();
        MailInboxRuntimeDatabaseOptions runtimeDatabaseOptions =
            scope.ServiceProvider.GetRequiredService<IOptions<MailInboxRuntimeDatabaseOptions>>().Value;
        IHostApplicationLifetime lifetime = scope.ServiceProvider.GetRequiredService<IHostApplicationLifetime>();

        await ExecuteAsync(
            command,
            readinessChecker,
            schemaInitializer,
            runtimeRoleProvisioner,
            runtimeDatabaseOptions,
            lifetime.ApplicationStopping).ConfigureAwait(false);
    }

    return 0;
} catch (OperationCanceledException) {
    Console.Error.WriteLine("MailInbox initializer canceled.");
    return 1;
} catch (Exception exception) {
    Console.Error.WriteLine($"MailInbox initializer failed. ErrorType={exception.GetType().Name}");
    return 1;
} finally {
    await host.StopAsync(CancellationToken.None).ConfigureAwait(false);
}

static async Task ExecuteAsync(
    InitializerCommand command,
    IMailInboxReadinessChecker readinessChecker,
    IMailInboxSchemaInitializer schemaInitializer,
    NpgsqlMailInboxRuntimeRoleProvisioner runtimeRoleProvisioner,
    MailInboxRuntimeDatabaseOptions runtimeDatabaseOptions,
    CancellationToken cancellationToken) {
    switch (command.Name) {
        case "status":
            await PrintStatusAsync(readinessChecker, cancellationToken).ConfigureAwait(false);
            break;
        case "update":
            Console.WriteLine("Updating MailInbox schema...");
            await schemaInitializer.EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);
            if (runtimeDatabaseOptions.ProvisionRole) {
                await runtimeRoleProvisioner.ProvisionAsync(
                    runtimeDatabaseOptions.RoleName,
                    runtimeDatabaseOptions.Password,
                    cancellationToken).ConfigureAwait(false);
            }

            Console.WriteLine("MailInbox schema update completed.");
            break;
        default:
            throw new InvalidOperationException($"Unknown command '{command.Name}'.");
    }
}

static async Task PrintStatusAsync(
    IMailInboxReadinessChecker readinessChecker,
    CancellationToken cancellationToken) {
    await readinessChecker.CheckReadyAsync(cancellationToken).ConfigureAwait(false);
    Console.WriteLine("Can connect:       True");
    Console.WriteLine("Schema ready:      True");
}

static void PrintUsage() {
    Console.WriteLine("""
Usage:
  dotnet run --project MailInbox/FoodDiary.MailInbox.Initializer -- <command>

Commands:
  status                  Show MailInbox schema status
  update                  Create or update MailInbox schema

Examples:
  dotnet run --project MailInbox/FoodDiary.MailInbox.Initializer -- status
  dotnet run --project MailInbox/FoodDiary.MailInbox.Initializer -- update

Configure ConnectionStrings__DefaultConnection through a protected environment or secret provider.
""");
}

[ExcludeFromCodeCoverage]
public partial class Program;
