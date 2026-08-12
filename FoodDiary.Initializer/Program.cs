using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using FoodDiary.Application;
using FoodDiary.Application.Billing;
using FoodDiary.Application.Dietologist;
using FoodDiary.Application.Marketing;
using FoodDiary.Application.Users;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Authentication.Commands.BootstrapInitialAdmin;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Outbox;
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
builder.Services.AddDietologistModule();
builder.Services.AddUsersModule();
builder.Services.AddBillingModule();
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
    IOutboxDeadLetterReplayService outboxReplayService =
        scope.ServiceProvider.GetRequiredService<IOutboxDeadLetterReplayService>();

    try {
        await ExecuteAsync(
            command,
            dbContext,
            initialAdminBootstrapService,
            outboxReplayService,
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
    IOutboxDeadLetterReplayService outboxReplayService,
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
        case "replay-outbox":
            await ReplayOutboxAsync(command, outboxReplayService).ConfigureAwait(false);
            break;
        case "list-dead-letters":
            await ListDeadLettersAsync(command, outboxReplayService).ConfigureAwait(false);
            break;
        case "show-dead-letter":
            await ShowDeadLetterAsync(command, outboxReplayService).ConfigureAwait(false);
            break;
        case "list-outbox-replays":
            await ListOutboxReplaysAsync(command, outboxReplayService).ConfigureAwait(false);
            break;
        default:
            throw new InvalidOperationException($"Unknown command '{command.Name}'.");
    }
}

static async Task ReplayOutboxAsync(
    InitializerCommand command,
    IOutboxDeadLetterReplayService replayService) {
    (string outboxName, Guid messageId) = ParseOutboxTarget(command, "replay-outbox");

    if (string.IsNullOrWhiteSpace(command.RequestedBy) || string.IsNullOrWhiteSpace(command.Reason)) {
        throw new InvalidOperationException("replay-outbox requires --requested-by and --reason.");
    }

    OutboxDeadLetterMessageModel message = await replayService
        .GetDeadLetterAsync(outboxName, messageId)
        .ConfigureAwait(false)
        ?? throw new InvalidOperationException("Dead-lettered outbox message was not found.");
    PrintDeadLetter(message);
    if (command.DryRun) {
        Console.WriteLine("Dry run only. No state was changed.");
        return;
    }
    if (!command.Force) {
        throw new InvalidOperationException("replay-outbox requires --force after inspecting the message or using --dry-run.");
    }
    if (!command.ExpectedAttemptCount.HasValue) {
        throw new InvalidOperationException("replay-outbox requires --expected-attempt-count from the inspected message.");
    }

    OutboxReplayAuditModel audit = await replayService.ReplayAsync(
        outboxName,
        messageId,
        command.RequestedBy,
        command.Reason,
        command.ExpectedAttemptCount.Value).ConfigureAwait(false);
    Console.WriteLine($"Replayed {audit.OutboxName} outbox message {audit.MessageId}. Audit id: {audit.Id}.");
}

static async Task ListDeadLettersAsync(
    InitializerCommand command,
    IOutboxDeadLetterReplayService replayService) {
    IReadOnlyList<OutboxDeadLetterMessageModel> messages = await replayService
        .ListDeadLettersAsync(command.TargetMigration, command.Limit)
        .ConfigureAwait(false);
    foreach (OutboxDeadLetterMessageModel message in messages) {
        PrintDeadLetter(message);
    }
    Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Dead-letter messages: {messages.Count}."));
}

static async Task ShowDeadLetterAsync(
    InitializerCommand command,
    IOutboxDeadLetterReplayService replayService) {
    (string outboxName, Guid messageId) = ParseOutboxTarget(command, "show-dead-letter");
    OutboxDeadLetterMessageModel? message = await replayService
        .GetDeadLetterAsync(outboxName, messageId)
        .ConfigureAwait(false);
    if (message is null) {
        throw new InvalidOperationException("Dead-lettered outbox message was not found.");
    }
    PrintDeadLetter(message);
}

static async Task ListOutboxReplaysAsync(
    InitializerCommand command,
    IOutboxDeadLetterReplayService replayService) {
    string[] parts = command.TargetMigration?.Split(':', 2) ?? [];
    string? outboxName = parts.Length > 0 ? parts[0] : null;
    Guid? messageId = parts.Length == 2 && Guid.TryParse(parts[1], out Guid parsedMessageId)
        ? parsedMessageId
        : null;
    IReadOnlyList<OutboxReplayAuditModel> audits = await replayService
        .ListReplayHistoryAsync(outboxName, messageId, command.Limit)
        .ConfigureAwait(false);
    foreach (OutboxReplayAuditModel audit in audits) {
        Console.WriteLine(
            string.Create(
                CultureInfo.InvariantCulture,
                $"{audit.RequestedOnUtc:O} {audit.OutboxName}:{audit.MessageId} attempts={audit.PreviousAttemptCount} by={audit.RequestedBy} reason={audit.Reason}"));
    }
    Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Replay audit records: {audits.Count}."));
}

static (string OutboxName, Guid MessageId) ParseOutboxTarget(InitializerCommand command, string commandName) {
    string[] targetParts = command.TargetMigration?.Split(':', 2) ?? [];
    if (targetParts.Length != 2 || !Guid.TryParse(targetParts[1], out Guid messageId)) {
        throw new InvalidOperationException(
                $"{commandName} requires target '<email|image_object_deletion|notification_web_push>:<message-id>'.");
    }
    return (targetParts[0], messageId);
}

static void PrintDeadLetter(OutboxDeadLetterMessageModel message) {
    Console.WriteLine(
        string.Create(
            CultureInfo.InvariantCulture,
            $"{message.DeadLetteredOnUtc:O} {message.OutboxName}:{message.MessageId} attempts={message.AttemptCount} summary={message.Summary}"));
    Console.WriteLine($"  error: {message.LastError ?? "<none>"}");
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
    string[] appliedMigrations = [.. await dbContext.Database.GetAppliedMigrationsAsync().ConfigureAwait(false)];
    string[] pendingMigrations = [.. await dbContext.Database.GetPendingMigrationsAsync().ConfigureAwait(false)];

    Console.WriteLine($"Can connect:       {canConnect}");
    Console.WriteLine($"Applied count:     {appliedMigrations.Length}");
    Console.WriteLine($"Pending count:     {pendingMigrations.Length}");
    Console.WriteLine($"Current migration: {appliedMigrations.LastOrDefault() ?? "<none>"}");

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
    string[] appliedMigrations = [.. await dbContext.Database.GetAppliedMigrationsAsync().ConfigureAwait(false)];
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
  list-dead-letters [outbox] [--limit <1-200>]
                          List dead-lettered messages across all or one outbox
  show-dead-letter <outbox>:<message-id>
                          Inspect one dead-lettered message before replay
  replay-outbox <outbox>:<message-id> --requested-by <operator> --reason <reason> --expected-attempt-count <count> [--dry-run | --force]
                          Preview or replay one dead-lettered message with race protection and audit
  list-outbox-replays [outbox[:message-id]] [--limit <1-200>]
                          List immutable replay audit history

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
