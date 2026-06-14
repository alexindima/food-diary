using System.Diagnostics.CodeAnalysis;
using FoodDiary.MailRelay.Application;
using FoodDiary.MailRelay.Infrastructure.Extensions;
using FoodDiary.MailRelay.Presentation.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

#if DEBUG
if (!builder.Environment.IsDevelopment()) {
    builder.Configuration.AddUserSecrets<Program>(optional: true);
}
#endif

builder.Services
    .AddMailRelayOptions(builder.Configuration)
    .AddMailRelayApplication()
    .AddMailRelayPresentation()
    .AddMailRelayServices(builder.Configuration)
    .AddMailRelayTelemetry();

WebApplication app = builder.Build();

app.MapMailRelayPresentation();

await app.RunAsync().ConfigureAwait(false);

[ExcludeFromCodeCoverage]
public partial class Program;
