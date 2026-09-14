using FoodDiary.Modules.Admin.Application.Abstractions.Common;
using FoodDiary.Modules.Admin.Infrastructure.Integrations.MailInbox;
using FoodDiary.MailInbox.Client.Extensions;
using FoodDiary.MailInbox.Client.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using FoodDiary.Mediator;
using FoodDiary.Modules.Admin.Contracts.Commands.SendBugAcknowledgements;
using FoodDiary.Modules.Admin.Application.Commands.SendBugAcknowledgements;

namespace FoodDiary.Modules.Admin.Infrastructure.Integrations;

public static class AdminMailInboxIntegration {
    private static readonly string[] ConfigurationKeys = [
        "BaseUrl",
        "MetadataApiKey",
        "ContentApiKey",
        "StateApiKey",
        "AllowInsecureLoopback",
    ];

    public static IServiceCollection AddAdminMailInboxIntegration(
        this IServiceCollection services,
        IConfiguration configuration) {
        IConfigurationSection section = configuration.GetSection(MailInboxClientOptions.SectionName);
        if (ConfigurationKeys.Any(key => section[key] is not null)) {
            services.AddMailInboxClient(options => {
                options.BaseUrl = section["BaseUrl"] ?? string.Empty;
                options.MetadataApiKey = section["MetadataApiKey"] ?? string.Empty;
                options.ContentApiKey = section["ContentApiKey"] ?? string.Empty;
                options.StateApiKey = section["StateApiKey"] ?? string.Empty;
                options.Timeout = TimeSpan.FromSeconds(15);
                options.AllowInsecureLoopback = section.GetValue<bool>("AllowInsecureLoopback");
            });
            services.AddScoped<IAdminMailInboxReader, MailInboxClientAdminMailInboxReader>();
            services.AddHttpClient<FoodDiary.MailInbox.Client.Export.IMailInboxExportClient, FoodDiary.MailInbox.Client.Export.MailInboxExportClient>((provider, client) => {
                MailInboxClientOptions options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<MailInboxClientOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = options.Timeout;
            }).ConfigurePrimaryHttpMessageHandler(static () => new HttpClientHandler { AllowAutoRedirect = false });
            services.AddFoodDiaryMediator(_ => { });
            services.TryAddEnumerable(ServiceDescriptor.Transient<IRequestHandler<SendBugAcknowledgementsCommand, Unit>, SendBugAcknowledgementsCommandHandler>());
            services.AddScoped<IBugAcknowledgementReceipts, BugAcknowledgementReceipts>();
            services.AddScoped<FoodDiary.Modules.Admin.Application.Abstractions.Common.IBugAcknowledgementSource, BugAcknowledgementSource>();
            services.AddOptions<BugAcknowledgementOptions>().Bind(configuration.GetSection("BugAcknowledgement"))
                .Validate(x => x.PollInterval >= TimeSpan.FromSeconds(10) && x.PollInterval <= TimeSpan.FromDays(1), "Invalid acknowledgement poll interval.")
                .ValidateOnStart();
            services.AddHostedService<BugAcknowledgementWorker>();
        }

        return services;
    }
}
