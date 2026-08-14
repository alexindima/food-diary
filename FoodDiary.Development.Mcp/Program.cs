using FoodDiary.Development.Mcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
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
