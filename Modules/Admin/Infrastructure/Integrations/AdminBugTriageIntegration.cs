using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Infrastructure.Integrations.BugTriage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FoodDiary.Infrastructure.Integrations;

public static class AdminBugTriageIntegration {
    public static IServiceCollection AddAdminBugTriageIntegration(this IServiceCollection services, IConfiguration configuration) {
        services.AddOptions<AdminBugTriageOptions>().Bind(configuration.GetSection("AdminBugTriage"))
            .Validate(AdminBugTriageOptions.IsValid, "A trusted HTTPS endpoint and a read-only BugTriage key are required.").ValidateOnStart();
        services.AddHttpClient<IAdminBugReportReader, AdminBugReportReader>((provider, client) => {
            AdminBugTriageOptions settings = provider.GetRequiredService<IOptions<AdminBugTriageOptions>>().Value;
            if (settings.BaseUrl.Length > 0) { client.BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/"); }
            client.Timeout = TimeSpan.FromSeconds(15);
            client.MaxResponseContentBufferSize = 2 * 1024 * 1024;
        }).ConfigurePrimaryHttpMessageHandler(static () => new HttpClientHandler { AllowAutoRedirect = false });
        return services;
    }
}
