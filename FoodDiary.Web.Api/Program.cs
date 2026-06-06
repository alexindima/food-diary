using FoodDiary.Web.Api.Extensions;
using System.Diagnostics.CodeAnalysis;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options => {
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB
});

builder.Services.AddApiServices(builder.Configuration);

WebApplication app = builder.Build();
app.UseApiPipeline();

app.Run();

[ExcludeFromCodeCoverage]
public partial class Program;
