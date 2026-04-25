using FoodDiary.MailInbox.Application;
using FoodDiary.MailInbox.Infrastructure.Extensions;
using FoodDiary.MailInbox.Presentation.Extensions;

var builder = WebApplication.CreateBuilder(args);

#if DEBUG
if (!builder.Environment.IsDevelopment()) {
    builder.Configuration.AddUserSecrets<Program>(optional: true);
}
#endif

builder.Services
    .AddMailInboxApplication()
    .AddMailInboxPresentation()
    .AddMailInboxInfrastructure(builder.Configuration);

var app = builder.Build();

app.MapMailInboxPresentation();

app.Run();

public partial class Program;
