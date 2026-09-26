using FoodDiary.Email.Contracts.Email.Common;
using FoodDiary.MailRelay.Client.Extensions;
using FoodDiary.MailRelay.Client.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Email.MailRelay;

public static class MailRelayDependencyInjection {
    public static IServiceCollection AddMailRelayIntegration(
        this IServiceCollection services,
        IConfiguration configuration) {
        services.AddMailRelayClient(options => {
            IConfigurationSection section = configuration.GetSection(MailRelayClientOptions.SectionName);
            options.BaseUrl = section["BaseUrl"] ?? string.Empty;
            options.ApiKey = section["ApiKey"] ?? string.Empty;
            options.Timeout = TimeSpan.FromSeconds(15);
            options.AllowInsecureHttp = section.GetValue<bool>("AllowInsecureHttp");
        });
        services.AddScoped<IOutgoingEmailJournal, RelayEmailJournal>();
        services.AddScoped<RelayEmailTransport>();
        services.AddScoped<IEmailTransport>(static provider => provider.GetRequiredService<RelayEmailTransport>());
        return services;
    }
}
