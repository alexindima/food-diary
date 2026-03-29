using System.Net.Sockets;
using FoodDiary.Infrastructure.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace FoodDiary.Web.Api.HealthChecks;

internal sealed class SmtpHealthCheck(IOptions<EmailOptions> emailOptions) : IHealthCheck {
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default) {
        var options = emailOptions.Value;

        if (string.IsNullOrWhiteSpace(options.SmtpHost)) {
            return HealthCheckResult.Healthy("SMTP not configured.");
        }

        try {
            using var client = new TcpClient();
            await client.ConnectAsync(options.SmtpHost, options.SmtpPort, cancellationToken);
            return HealthCheckResult.Healthy();
        } catch (Exception ex) {
            return HealthCheckResult.Unhealthy($"SMTP unreachable: {options.SmtpHost}:{options.SmtpPort}", ex);
        }
    }
}
