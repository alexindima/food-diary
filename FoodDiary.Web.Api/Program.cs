using FoodDiary.Web.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options => {
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB
});

builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();
app.UseApiPipeline();

app.Run();

public partial class Program;
