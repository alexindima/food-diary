using FoodDiary.Application;
using FoodDiary.Application.Marketing;
using FoodDiary.Infrastructure;
using FoodDiary.Integrations;
using FoodDiary.JobManager.Services;
using Hangfire;
using Hangfire.PostgreSql;
using System.Diagnostics.CodeAnalysis;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplication();
builder.Services.AddMarketingModule();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddIntegrations(builder.Configuration);
builder.Services.AddNotificationResources();
builder.Services.AddJobManagerServices(builder.Configuration);

builder.Services.AddHangfire((sp, config) => {
    string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                           ?? throw new InvalidOperationException("DefaultConnection is not configured.");

    config
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString));
});
builder.Services.AddHangfireServer();
builder.Services.AddSingleton<IRecurringJobRegistrationVerifier, HangfireRecurringJobRegistrationVerifier>();
builder.Services.AddHostedService<RecurringJobsHostedService>();

IHost app = builder.Build();

await app.RunAsync().ConfigureAwait(false);

[ExcludeFromCodeCoverage]
public partial class Program;
