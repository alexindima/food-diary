using FoodDiary.MailInbox.Application;
using FoodDiary.MailInbox.Infrastructure.Extensions;
using FoodDiary.MailInbox.Presentation.Extensions;
using System.Diagnostics.CodeAnalysis;

var builder = WebApplication.CreateBuilder(args);

#if DEBUG
if (!builder.Environment.IsDevelopment()) {
    builder.Configuration.AddUserSecrets<Program>(optional: true);
}
#endif

builder.Services
    .AddMailInboxApplication()
    .AddMailInboxPresentation(builder.Configuration)
    .AddMailInboxInfrastructure(builder.Configuration);

var app = builder.Build();

app.MapMailInboxPresentation();

app.Run();

[ExcludeFromCodeCoverage]
public partial class Program;
