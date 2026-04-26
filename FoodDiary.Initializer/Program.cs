using FoodDiary.Application;
using FoodDiary.Application.Authentication.Common;
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

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddEnvironmentVariables("FOODDIARY_");

var webApiSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "FoodDiary.Web.Api");
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
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> {
        ["ConnectionStrings:DefaultConnection"] = command.ConnectionString
    });
}

if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("DefaultConnection"))) {
    Console.Error.WriteLine(
        "Initializer failed: DefaultConnection is not configured. Pass --connection-string, set FOODDIARY_ConnectionStrings__DefaultConnection, or provide appsettings in FoodDiary.Web.Api.");
    return 1;
}

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddDistributedMemoryCache();
builder.Services.AddScoped<IEmailVerificationNotifier, NoOpEmailVerificationNotifier>();
builder.Services.AddScoped<INotificationPusher, NoOpNotificationPusher>();

using var host = builder.Build();
using var scope = host.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();

try {
    await ExecuteAsync(command, dbContext);
    return 0;
} catch (Exception exception) {
    Console.Error.WriteLine($"Initializer failed: {exception}");
    return 1;
}

static async Task ExecuteAsync(InitializerCommand command, FoodDiaryDbContext dbContext) {
    switch (command.Name) {
        case "list":
            await ListMigrationsAsync(dbContext);
            break;
        case "status":
            await PrintStatusAsync(dbContext);
            break;
        case "update":
            await UpdateDatabaseAsync(dbContext, command.TargetMigration);
            break;
        case "rollback":
            await RollbackDatabaseAsync(dbContext, command.TargetMigration);
            break;
        case "rollback-last":
            await RollbackLastMigrationAsync(dbContext);
            break;
        case "seed-usda":
            if (string.IsNullOrWhiteSpace(command.TargetMigration)) {
                throw new InvalidOperationException("seed-usda requires a path to the USDA CSV directory.");
            }
            if (command.Force) {
                await UsdaDataSeeder.ForceSeedAsync(dbContext, command.TargetMigration);
            } else {
                await UsdaDataSeeder.SeedAsync(dbContext, command.TargetMigration);
            }
            break;
        default:
            throw new InvalidOperationException($"Unknown command '{command.Name}'.");
    }
}

static async Task ListMigrationsAsync(FoodDiaryDbContext dbContext) {
    var migrationsAssembly = dbContext.Database.GetInfrastructure().GetRequiredService<IMigrationsAssembly>();
    var allMigrations = migrationsAssembly.Migrations.Keys;
    var appliedMigrations = new HashSet<string>(await dbContext.Database.GetAppliedMigrationsAsync(), StringComparer.OrdinalIgnoreCase);

    foreach (var migration in allMigrations) {
        var state = appliedMigrations.Contains(migration) ? "applied" : "pending";
        Console.WriteLine($"{state,-8} {migration}");
    }
}

static async Task PrintStatusAsync(FoodDiaryDbContext dbContext) {
    var canConnect = await dbContext.Database.CanConnectAsync();
    var appliedMigrations = (await dbContext.Database.GetAppliedMigrationsAsync()).ToArray();
    var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync()).ToArray();

    Console.WriteLine($"Can connect:       {canConnect}");
    Console.WriteLine($"Applied count:     {appliedMigrations.Length}");
    Console.WriteLine($"Pending count:     {pendingMigrations.Length}");
    Console.WriteLine($"Current migration: {(appliedMigrations.LastOrDefault() ?? "<none>")}");

    if (pendingMigrations.Length > 0) {
        Console.WriteLine("Pending migrations:");
        foreach (var migration in pendingMigrations) {
            Console.WriteLine($"  {migration}");
        }
    }
}

static async Task UpdateDatabaseAsync(FoodDiaryDbContext dbContext, string? targetMigration) {
    var migrator = dbContext.Database.GetInfrastructure().GetRequiredService<IMigrator>();
    var destination = string.IsNullOrWhiteSpace(targetMigration) ? "<latest>" : targetMigration;

    Console.WriteLine($"Updating database to {destination}...");
    await migrator.MigrateAsync(targetMigration);
    Console.WriteLine("Database update completed.");
}

static async Task RollbackDatabaseAsync(FoodDiaryDbContext dbContext, string? targetMigration) {
    if (string.IsNullOrWhiteSpace(targetMigration)) {
        throw new InvalidOperationException("Rollback requires a target migration or 0.");
    }

    var migrator = dbContext.Database.GetInfrastructure().GetRequiredService<IMigrator>();

    Console.WriteLine($"Rolling database back to {targetMigration}...");
    await migrator.MigrateAsync(targetMigration);
    Console.WriteLine("Database rollback completed.");
}

static async Task RollbackLastMigrationAsync(FoodDiaryDbContext dbContext) {
    var appliedMigrations = (await dbContext.Database.GetAppliedMigrationsAsync()).ToArray();
    if (appliedMigrations.Length == 0) {
        Console.WriteLine("Database has no applied migrations.");
        return;
    }

    var targetMigration = appliedMigrations.Length == 1 ? "0" : appliedMigrations[^2];
    await RollbackDatabaseAsync(dbContext, targetMigration);
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

internal sealed record InitializerCommand(string Name, string? TargetMigration, string? ConnectionString, bool Force = false) {
    public static InitializerCommand? Parse(string[] args) {
        if (args.Length == 0) {
            return null;
        }

        string? name = null;
        string? targetMigration = null;
        string? connectionString = null;
        var force = false;

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

            if (argument is "--force" or "-f") {
                force = true;
                continue;
            }

            if (name is null) {
                name = argument;
                continue;
            }

            if (targetMigration is null) {
                targetMigration = argument;
                continue;
            }

            throw new InvalidOperationException($"Unexpected argument '{argument}'.");
        }

        return name is null ? null : new InitializerCommand(name, targetMigration, connectionString, force);
    }
}

internal sealed class NoOpEmailVerificationNotifier : IEmailVerificationNotifier {
    public Task NotifyEmailVerifiedAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

internal sealed class NoOpNotificationPusher : INotificationPusher {
    public Task PushUnreadCountAsync(Guid userId, int count, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
    public Task PushNotificationsChangedAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
