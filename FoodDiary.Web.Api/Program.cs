using System.Diagnostics.CodeAnalysis;
using FoodDiary.Web.Api.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddTaskConfiguration(
    builder.Environment,
    Environment.GetEnvironmentVariable("FOODDIARY_TASK_CONFIG"));

builder.WebHost.ConfigureKestrel(options => {
    options.Limits.MaxRequestBodySize = 1024 * 1024; // 1 MB by default; larger endpoints opt in explicitly.
});

builder.Services.AddApiServices(builder.Configuration, builder.Environment);
if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("FOODDIARY_TASK_CONFIG"))) {
    builder.Services.AddTaskOutboundGuard();
}

WebApplication app = builder.Build();
app.UseApiPipeline();

await app.RunAsync().ConfigureAwait(false);

[ExcludeFromCodeCoverage]
public partial class Program;
