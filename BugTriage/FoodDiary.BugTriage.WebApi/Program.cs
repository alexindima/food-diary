using FoodDiary.BugTriage.Infrastructure.Extensions;
using FoodDiary.BugTriage.Infrastructure.Persistence;
using FoodDiary.BugTriage.Presentation.Extensions;
using Npgsql;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddBugTriageInfrastructure(builder.Configuration);
builder.Services.AddBugTriagePresentation(builder.Configuration);
WebApplication app = builder.Build();
if (args is ["--initialize"]) {
    await BugTriageSchema.InitializeAsync(app.Services.GetRequiredService<NpgsqlDataSource>(), CancellationToken.None).ConfigureAwait(false);
    return;
}
app.Use(async (context, next) => {
    try {
        await next(context).ConfigureAwait(false);
    } catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested) {
        context.Abort();
    } catch (Exception) when (!context.Response.HasStarted) {
        // Avoid default exception logging: database/provider messages can contain private content.
        context.Response.Clear();
        context.Response.StatusCode = 503;
        await context.Response.WriteAsJsonAsync(new { error = "Service temporarily unavailable." }, context.RequestAborted).ConfigureAwait(false);
    }
});
app.UseRouting();
app.UseRateLimiter();
app.MapControllers();
await app.RunAsync().ConfigureAwait(false);

public partial class Program;
