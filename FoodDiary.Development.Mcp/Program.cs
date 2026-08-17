using FoodDiary.Development.Mcp.ChangeSets;
using FoodDiary.Development.Mcp.Diagnostics;
using FoodDiary.Development.Mcp.Tools;
using FoodDiary.Development.Mcp.Wiki;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
string? sessionLockPath = Environment.GetEnvironmentVariable("FOODDIARY_MCP_SESSION_LOCK");
FileStream? sessionLock = string.IsNullOrWhiteSpace(sessionLockPath)
    ? null
    : File.Open(sessionLockPath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read);
string repositoryRoot = FoodDiary.Development.Mcp.Infrastructure.RepositoryRootResolver.Resolve();
string repositoryHeadAtStartup = await ServerStatusService
    .ReadGitHeadAsync(repositoryRoot, CancellationToken.None)
    .ConfigureAwait(false);

builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(ServerRuntimeIdentity.Capture(repositoryHeadAtStartup));
builder.Services.AddSingleton<WikiRuntimeTelemetry>();
builder.Services.AddSingleton<WikiQueryCache>();
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
