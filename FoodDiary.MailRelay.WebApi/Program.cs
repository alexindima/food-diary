using FoodDiary.MailRelay.Application;
using FoodDiary.MailRelay.Infrastructure.Extensions;
using FoodDiary.MailRelay.Presentation.Extensions;

var builder = WebApplication.CreateBuilder(args);

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

var app = builder.Build();

app.MapMailRelayPresentation();

app.Run();

public partial class Program;
