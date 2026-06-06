using FoodDiary.MailInbox.Application;
using FoodDiary.MailInbox.Infrastructure.Extensions;
using FoodDiary.MailInbox.Presentation.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

#if DEBUG
if (!builder.Environment.IsDevelopment()) {
    builder.Configuration.AddUserSecrets<Program>(optional: true);
}
#endif

builder.Services
    .AddMailInboxApplication()
    .AddMailInboxPresentation(builder.Configuration)
    .AddMailInboxInfrastructure(builder.Configuration);

WebApplication app = builder.Build();

app.MapMailInboxPresentation();

await app.RunAsync().ConfigureAwait(false);
