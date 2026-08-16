using FoodDiary.Development.Mcp.ChangeSets;
using FoodDiary.Development.Mcp.Diagnostics;
using FoodDiary.Development.Mcp.Tools;
using FoodDiary.Development.Mcp.Wiki;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
string repositoryRoot = FoodDiary.Development.Mcp.Infrastructure.RepositoryRootResolver.Resolve();
string repositoryHeadAtStartup = await ServerStatusService
    .ReadGitHeadAsync(repositoryRoot, CancellationToken.None)
    .ConfigureAwait(false);

builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(ServerRuntimeIdentity.Capture(repositoryHeadAtStartup));
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

await builder.Build().RunAsync().ConfigureAwait(false);
