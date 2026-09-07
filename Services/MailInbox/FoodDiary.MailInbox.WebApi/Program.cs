using FoodDiary.MailInbox.Application;
using FoodDiary.MailInbox.Infrastructure.Extensions;
using FoodDiary.MailInbox.Infrastructure.Services;
using FoodDiary.MailInbox.Presentation.Extensions;
using FoodDiary.MailInbox.WebApi;
using System.Diagnostics.CodeAnalysis;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

if (args is ["--healthcheck"]) {
    Environment.ExitCode = await MailInboxLocalTlsHealthCheck
        .IsReadyAsync(builder.Configuration["MailInboxSmtp:ServerName"], CancellationToken.None)
        .ConfigureAwait(false)
        ? 0
        : 1;
    return;
}

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

await MailInboxHostConfiguration.ValidateRuntimeDatabaseRoleAsync(
        app.Services,
        app.Configuration,
        app.Environment,
        app.Lifetime.ApplicationStopping)
    .ConfigureAwait(false);

app.MapMailInboxPresentation();

await app.RunAsync().ConfigureAwait(false);

[ExcludeFromCodeCoverage]
public partial class Program;
