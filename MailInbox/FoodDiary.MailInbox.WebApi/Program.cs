using FoodDiary.MailInbox.Application;
using FoodDiary.MailInbox.Infrastructure.Extensions;
using FoodDiary.MailInbox.Presentation.Extensions;
using FoodDiary.MailInbox.WebApi;
using System.Diagnostics.CodeAnalysis;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

#if DEBUG
if (!builder.Environment.IsDevelopment()) {
    builder.Configuration.AddUserSecrets<Program>(optional: true);
}
#endif

builder.Services.AddMailInboxHostConfigurationValidation();

builder.Services
    .AddMailInboxApplication()
    .AddMailInboxPresentation(builder.Configuration)
    .AddMailInboxInfrastructure(builder.Configuration)
    .AddMailInboxTelemetry(builder.Configuration);

WebApplication app = builder.Build();

app.MapMailInboxPresentation();

await app.RunAsync().ConfigureAwait(false);

[ExcludeFromCodeCoverage]
public partial class Program;
