using FoodDiary.Development.Mcp.ChangeSets;
using FoodDiary.Development.Mcp.Diagnostics;
using FoodDiary.Development.Mcp.Tools;
using FoodDiary.Development.Mcp.Wiki;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

if (args.Length == 2 && string.Equals(
    args[0],
    "--evaluate-context-search",
    StringComparison.Ordinal)) {
    WikiRuntimeTelemetry evaluationTelemetry = new();
    SqliteWikiContextSearch evaluationSearch = new(evaluationTelemetry);
    using ChangeSetSnapshotService evaluationSnapshots = new();
    ChangeSetSnapshot evaluationSnapshot = await evaluationSnapshots
        .GetAsync(CancellationToken.None)
        .ConfigureAwait(false);
    await WikiContextSearchEvaluationRunner.RunAsync(
        evaluationSearch,
        args[1],
        Console.Out,
        CancellationToken.None,
        evaluationSnapshot.Fingerprint).ConfigureAwait(false);
    return;
}

if (args.Length == 2 && string.Equals(
    args[0],
    "--evaluate-development-context-bundles",
    StringComparison.Ordinal)) {
    WikiRuntimeTelemetry evaluationTelemetry = new();
    using ChangeSetSnapshotService evaluationSnapshots = new();
    WikiQueryCache evaluationCache = new(TimeProvider.System, evaluationTelemetry);
    PowerShellWikiCommandExecutor evaluationExecutor = new(evaluationTelemetry);
    SqliteWikiContextSearch evaluationSearch = new(evaluationTelemetry);
    WikiQueryService evaluationQueries = new(
        evaluationExecutor,
        evaluationSnapshots,
        evaluationCache,
        evaluationSearch,
        evaluationTelemetry);
    await DevelopmentContextEvaluationRunner.RunAsync(
        evaluationQueries,
        args[1],
        Console.Out,
        CancellationToken.None).ConfigureAwait(false);
    return;
}

if (args.Length is 2 or 3 && string.Equals(
    args[0],
    "--record-context-routing-retirement-evidence",
    StringComparison.Ordinal)) {
    int? maximumCases = args.Length == 3
        ? int.Parse(args[2], System.Globalization.CultureInfo.InvariantCulture)
        : null;
    string evaluationRepositoryRoot = FoodDiary.Development.Mcp.Infrastructure.RepositoryRootResolver.Resolve();
    string evaluationGitDirectory = await ServerStatusService
        .ResolveGitDirectoryForStatusAsync(evaluationRepositoryRoot, CancellationToken.None)
        .ConfigureAwait(false);
    var evaluationRoutingStore = new ContextRoutingTelemetryStore(Path.Combine(
        evaluationGitDirectory,
        "llm-wiki",
        "context-routing-telemetry.json"));
    WikiRuntimeTelemetry evaluationTelemetry = new(evaluationRoutingStore);
    using ChangeSetSnapshotService evaluationSnapshots = new();
    WikiQueryCache evaluationCache = new(TimeProvider.System, evaluationTelemetry);
    PowerShellWikiCommandExecutor evaluationExecutor = new(evaluationTelemetry);
    SqliteWikiContextSearch evaluationSearch = new(evaluationTelemetry);
    WikiQueryService evaluationQueries = new(
        evaluationExecutor,
        evaluationSnapshots,
        evaluationCache,
        evaluationSearch,
        evaluationTelemetry);
    await ContextRoutingRetirementEvaluationRunner.RunAsync(
        evaluationQueries,
        evaluationRoutingStore,
        args[1],
        Console.Out,
        CancellationToken.None,
        maximumCases).ConfigureAwait(false);
    return;
}

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
string? sessionLockPath = Environment.GetEnvironmentVariable("FOODDIARY_MCP_SESSION_LOCK");
FileStream? sessionLock = string.IsNullOrWhiteSpace(sessionLockPath)
    ? null
    : File.Open(sessionLockPath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read);
string repositoryRoot = FoodDiary.Development.Mcp.Infrastructure.RepositoryRootResolver.Resolve();
string repositoryHeadAtStartup = await ServerStatusService
    .ReadGitHeadAsync(repositoryRoot, CancellationToken.None)
    .ConfigureAwait(false);
string gitDirectory = await ServerStatusService
    .ResolveGitDirectoryForStatusAsync(repositoryRoot, CancellationToken.None)
    .ConfigureAwait(false);
string contextRoutingTelemetryPath = Path.Combine(
    gitDirectory,
    "llm-wiki",
    "context-routing-telemetry.json");

builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(ServerRuntimeIdentity.Capture(repositoryHeadAtStartup));
builder.Services.AddSingleton(serviceProvider => new ContextRoutingTelemetryStore(
    contextRoutingTelemetryPath,
    serviceProvider.GetRequiredService<TimeProvider>()));
builder.Services.AddSingleton<WikiRuntimeTelemetry>();
builder.Services.AddSingleton<WikiQueryCache>();
builder.Services.AddSingleton<IWikiContextSearch, SqliteWikiContextSearch>();
builder.Services.AddSingleton<IServerStatusService, ServerStatusService>();
builder.Services.AddSingleton<IChangeSetSnapshotService, ChangeSetSnapshotService>();
builder.Services.AddSingleton<IWikiCommandExecutor, PowerShellWikiCommandExecutor>();
builder.Services.AddSingleton<WikiQueryService>();
builder.Services
    .AddMcpServer(options => options.ServerInstructions =
        "Use these tools first for FoodDiary wiki change context, backend traces, and test plans. " +
        "Treat results as derived navigation and verify change-sensitive claims in referenced code, tests, ADRs, " +
        "current docs, and scoped AGENTS.md. If this server is unavailable or incomplete, use .llm-wiki directly.")
    .WithStdioServerTransport()
    .WithTools<WikiTools>();

try {
    await builder.Build().RunAsync().ConfigureAwait(false);
} finally {
    if (sessionLock is not null) {
        await sessionLock.DisposeAsync().ConfigureAwait(false);
    }
}
