using FoodDiary.Infrastructure.Options;
using FoodDiary.Web.Api.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FoodDiary.Web.Api.IntegrationTests;

public sealed class HealthCheckTests {
    [Fact]
    public async Task SmtpHealthCheck_WhenNotConfigured_ReturnsHealthy() {
        var options = Microsoft.Extensions.Options.Options.Create(new EmailOptions { SmtpHost = "" });
        var check = new SmtpHealthCheck(options);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Contains("not configured", result.Description);
    }

    [Fact]
    public async Task SmtpHealthCheck_WhenHostUnreachable_ReturnsUnhealthy() {
        var options = Microsoft.Extensions.Options.Options.Create(new EmailOptions { SmtpHost = "192.0.2.1", SmtpPort = 9999 });
        var check = new SmtpHealthCheck(options);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("unreachable", result.Description);
    }
}
