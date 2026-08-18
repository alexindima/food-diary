using System.Diagnostics.CodeAnalysis;
using FoodDiary.Web.Api.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options => {
    options.Limits.MaxRequestBodySize = 1024 * 1024; // 1 MB by default; larger endpoints opt in explicitly.
});

builder.Services.AddApiServices(builder.Configuration, builder.Environment);

WebApplication app = builder.Build();
app.UseApiPipeline();

await app.RunAsync().ConfigureAwait(false);

[ExcludeFromCodeCoverage]
public partial class Program;
