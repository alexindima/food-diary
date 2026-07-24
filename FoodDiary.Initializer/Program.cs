using System.Diagnostics.CodeAnalysis;
using FoodDiary.Application;
using FoodDiary.Application.Marketing;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Authentication.Commands.BootstrapInitialAdmin;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Initializer;
using FoodDiary.Infrastructure;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var command = InitializerCommand.Parse(args);
if (command is null) {
    PrintUsage();
    return 1;
}

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddEnvironmentVariables("FOODDIARY_");

string webApiSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "FoodDiary.Web.Api");
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

if (!string.IsNullOrWhiteSpace(command.ConnectionString)) {
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
        ["ConnectionStrings:DefaultConnection"] = command.ConnectionString,
    });
}

if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("DefaultConnection"))) {
    Console.Error.WriteLine(
        "Initializer failed: DefaultConnection is not configured. Pass --connection-string, set FOODDIARY_ConnectionStrings__DefaultConnection, or provide appsettings in FoodDiary.Web.Api.");
    return 1;
}

builder.Services.AddApplication();
builder.Services.AddMarketingModule();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddDistributedMemoryCache();
builder.Services.AddScoped<IEmailVerificationNotifier, NoOpEmailVerificationNotifier>();
builder.Services.AddScoped<INotificationPusher, NoOpNotificationPusher>();

using IHost host = builder.Build();
AsyncServiceScope scope = host.Services.CreateAsyncScope();
await using (scope.ConfigureAwait(false)) {
    FoodDiaryDbContext dbContext = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
    IInitialAdminBootstrapService initialAdminBootstrapService =
        scope.ServiceProvider.GetRequiredService<IInitialAdminBootstrapService>();

    try {
        await ExecuteAsync(
            command,
            dbContext,
            initialAdminBootstrapService,
            builder.Configuration).ConfigureAwait(false);
        return 0;
    } catch (Exception exception) {
        Console.Error.WriteLine($"Initializer failed: {exception}");
        return 1;
    }
}

static async Task ExecuteAsync(
    InitializerCommand command,
    FoodDiaryDbContext dbContext,
    IInitialAdminBootstrapService initialAdminBootstrapService,
    IConfiguration configuration) {
    switch (command.Name) {
        case "list":
            await ListMigrationsAsync(dbContext).ConfigureAwait(false);
            break;
        case "status":
            await PrintStatusAsync(dbContext).ConfigureAwait(false);
            break;
        case "update":
            await UpdateDatabaseAsync(dbContext, command.TargetMigration).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(command.TargetMigration)) {
                await InitialAdminBootstrapper.BootstrapAsync(
                    initialAdminBootstrapService,
                    InitialAdminBootstrapOptions.FromConfiguration(configuration)).ConfigureAwait(false);
            }
            break;
        case "rollback":
            await RollbackDatabaseAsync(dbContext, command.TargetMigration).ConfigureAwait(false);
            break;
        case "rollback-last":
            await RollbackLastMigrationAsync(dbContext).ConfigureAwait(false);
            break;
        case "seed-usda":
            if (string.IsNullOrWhiteSpace(command.TargetMigration)) {
                throw new InvalidOperationException("seed-usda requires a path to the USDA CSV directory.");
            }
            if (command.Force) {
                await UsdaDataSeeder.ForceSeedAsync(dbContext, command.TargetMigration).ConfigureAwait(false);
            } else {
                await UsdaDataSeeder.SeedAsync(dbContext, command.TargetMigration).ConfigureAwait(false);
            }
            break;
        default:
            throw new InvalidOperationException($"Unknown command '{command.Name}'.");
    }
}

static async Task ListMigrationsAsync(FoodDiaryDbContext dbContext) {
    IMigrationsAssembly migrationsAssembly = dbContext.Database.GetInfrastructure().GetRequiredService<IMigrationsAssembly>();
    IEnumerable<string> allMigrations = migrationsAssembly.Migrations.Keys;
    var appliedMigrations = new HashSet<string>(await dbContext.Database.GetAppliedMigrationsAsync().ConfigureAwait(false), StringComparer.OrdinalIgnoreCase);

    foreach (string migration in allMigrations) {
        string state = appliedMigrations.Contains(migration) ? "applied" : "pending";
        Console.WriteLine($"{state,-8} {migration}");
    }
}

static async Task PrintStatusAsync(FoodDiaryDbContext dbContext) {
    bool canConnect = await dbContext.Database.CanConnectAsync().ConfigureAwait(false);
    string[] appliedMigrations = [.. (await dbContext.Database.GetAppliedMigrationsAsync().ConfigureAwait(false))];
    string[] pendingMigrations = [.. (await dbContext.Database.GetPendingMigrationsAsync().ConfigureAwait(false))];

    Console.WriteLine($"Can connect:       {canConnect}");
    Console.WriteLine($"Applied count:     {appliedMigrations.Length}");
    Console.WriteLine($"Pending count:     {pendingMigrations.Length}");
    Console.WriteLine($"Current migration: {(appliedMigrations.LastOrDefault() ?? "<none>")}");

    if (pendingMigrations.Length > 0) {
        Console.WriteLine("Pending migrations:");
        foreach (string migration in pendingMigrations) {
            Console.WriteLine($"  {migration}");
        }
    }
}

static async Task UpdateDatabaseAsync(FoodDiaryDbContext dbContext, string? targetMigration) {
    IMigrator migrator = dbContext.Database.GetInfrastructure().GetRequiredService<IMigrator>();
    string destination = string.IsNullOrWhiteSpace(targetMigration) ? "<latest>" : targetMigration;

    Console.WriteLine($"Updating database to {destination}...");
    await migrator.MigrateAsync(targetMigration).ConfigureAwait(false);
    Console.WriteLine("Database update completed.");
}

static async Task RollbackDatabaseAsync(FoodDiaryDbContext dbContext, string? targetMigration) {
    if (string.IsNullOrWhiteSpace(targetMigration)) {
        throw new InvalidOperationException("Rollback requires a target migration or 0.");
    }

    IMigrator migrator = dbContext.Database.GetInfrastructure().GetRequiredService<IMigrator>();

    Console.WriteLine($"Rolling database back to {targetMigration}...");
    await migrator.MigrateAsync(targetMigration).ConfigureAwait(false);
    Console.WriteLine("Database rollback completed.");
}

static async Task RollbackLastMigrationAsync(FoodDiaryDbContext dbContext) {
    string[] appliedMigrations = [.. (await dbContext.Database.GetAppliedMigrationsAsync().ConfigureAwait(false))];
    if (appliedMigrations.Length == 0) {
        Console.WriteLine("Database has no applied migrations.");
        return;
    }

    string targetMigration = appliedMigrations.Length == 1 ? "0" : appliedMigrations[^2];
    await RollbackDatabaseAsync(dbContext, targetMigration).ConfigureAwait(false);
}

static void PrintUsage() {
    Console.WriteLine("""
Usage:
  dotnet run --project FoodDiary.Initializer -- <command> [target] [--connection-string "<value>"] [--force]

Commands:
  list                    List all migrations with applied/pending state
  status                  Show current migration status
  update [target]         Apply migrations up to target or latest when omitted
  rollback-last           Roll database back by one migration
  rollback <target|0>     Roll database back to a specific migration or 0
  seed-usda <csv-dir>     Import USDA SR Legacy data from CSV files (--force to re-seed)

Examples:
  dotnet run --project FoodDiary.Initializer -- status
  dotnet run --project FoodDiary.Initializer -- update
  dotnet run --project FoodDiary.Initializer -- rollback-last
  dotnet run --project FoodDiary.Initializer -- rollback 20260209005246_AddShoppingLists
  dotnet run --project FoodDiary.Initializer -- update --connection-string "Host=..."
  dotnet run --project FoodDiary.Initializer -- seed-usda ./usda-data
  dotnet run --project FoodDiary.Initializer -- seed-usda ./usda-data --force
""");
}

[ExcludeFromCodeCoverage]
public partial class Program;
