using FoodDiary.Application;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Infrastructure;
using FoodDiary.JobManager.Services;
using Hangfire;
using Hangfire.PostgreSql;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.Configure<ImageCleanupOptions>(builder.Configuration.GetSection(ImageCleanupOptions.SectionName));
builder.Services.Configure<UserCleanupOptions>(builder.Configuration.GetSection(UserCleanupOptions.SectionName));

builder.Services.AddHangfire((sp, config) =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                           ?? throw new InvalidOperationException("DefaultConnection is not configured.");

    config
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString));
});
builder.Services.AddHangfireServer();

builder.Services.AddSingleton<ImageCleanupJob>();
builder.Services.AddSingleton<UserCleanupJob>();
builder.Services.AddHostedService<RecurringJobsHostedService>();

var app = builder.Build();

await app.RunAsync();
