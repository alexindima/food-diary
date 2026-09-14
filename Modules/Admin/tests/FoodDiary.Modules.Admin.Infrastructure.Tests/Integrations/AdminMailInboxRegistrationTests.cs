using FoodDiary.Modules.Admin.Application.Abstractions.Common;
using FoodDiary.Modules.Admin.Application;
using FoodDiary.Modules.Admin.Application.Commands.SendBugAcknowledgements;
using FoodDiary.Modules.Admin.Infrastructure.Integrations;
using FoodDiary.Modules.Admin.Infrastructure.Integrations.MailInbox;
using FoodDiary.MailInbox.Client.Export;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FoodDiary.Modules.Admin.Infrastructure.Tests.Integrations;

[ExcludeFromCodeCoverage]
public sealed class AdminMailInboxRegistrationTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MailIntegrationAndApplication_RegisterAcknowledgementHandlerOnce(bool applicationFirst) {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["MailInboxClient:BaseUrl"] = "https://inbox.example.com",
        }).Build();
        var services = new ServiceCollection();
        if (applicationFirst) {
            services.AddAdminApplication();
        }
        services.AddAdminMailInboxIntegration(configuration);
        if (!applicationFirst) {
            services.AddAdminApplication();
        }

        Assert.Single(services, descriptor => descriptor.ImplementationType == typeof(SendBugAcknowledgementsCommandHandler));
    }

    [Theory]
    [InlineData("00:00:10", true)]
    [InlineData("1.00:00:00", true)]
    [InlineData("00:00:09", false)]
    [InlineData("1.00:00:01", false)]
    public void Registration_ConfiguresExportClientWorkerAndValidatesPolling(string interval, bool valid) {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["MailInboxClient:BaseUrl"] = "https://inbox.example.com",
            ["MailInboxClient:MetadataApiKey"] = new string('m', 32),
            ["MailInboxClient:ContentApiKey"] = new string('c', 32),
            ["MailInboxClient:StateApiKey"] = new string('s', 32),
            ["BugAcknowledgement:PollInterval"] = interval,
        }).Build();
        var services = new ServiceCollection();
        services.AddAdminMailInboxIntegration(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.IsType<MailInboxClientAdminMailInboxReader>(provider.GetRequiredService<IAdminMailInboxReader>());
        Assert.IsType<MailInboxExportClient>(provider.GetRequiredService<IMailInboxExportClient>());
        Assert.IsType<BugAcknowledgementWorker>(Assert.Single(provider.GetServices<IHostedService>()));
        if (valid) {
            Assert.Equal(TimeSpan.Parse(interval, System.Globalization.CultureInfo.InvariantCulture), provider.GetRequiredService<IOptions<BugAcknowledgementOptions>>().Value.PollInterval);
        } else {
            Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<BugAcknowledgementOptions>>().Value);
        }
    }
}
