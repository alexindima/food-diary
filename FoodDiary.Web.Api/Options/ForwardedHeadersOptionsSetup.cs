using System.Diagnostics.CodeAnalysis;
using NetIPNetwork = System.Net.IPNetwork;
using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

namespace FoodDiary.Web.Api.Options;

[ExcludeFromCodeCoverage]
public sealed class ForwardedHeadersOptionsSetup(IOptions<ApiForwardedHeadersOptions> apiForwardedHeadersOptions)
    : IConfigureOptions<ForwardedHeadersOptions> {
    public void Configure(ForwardedHeadersOptions options) {
        ApiForwardedHeadersOptions settings = apiForwardedHeadersOptions.Value;

        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                                   ForwardedHeaders.XForwardedHost |
                                   ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = settings.ForwardLimit;
        options.AllowedHosts.Clear();
        foreach (string allowedHost in settings.AllowedHosts) {
            options.AllowedHosts.Add(allowedHost);
        }

        if (settings.KnownProxies.Length == 0 && settings.KnownNetworks.Length == 0) {
            return;
        }

        options.KnownProxies.Clear();
        options.KnownIPNetworks.Clear();
        foreach (string proxy in settings.KnownProxies) {
            options.KnownProxies.Add(IPAddress.Parse(proxy));
        }

        foreach (string network in settings.KnownNetworks) {
            options.KnownIPNetworks.Add(ParseNetwork(network));
        }
    }

    private static NetIPNetwork ParseNetwork(string cidr) {
        string[] parts = cidr.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return new NetIPNetwork(IPAddress.Parse(parts[0]), int.Parse(parts[1], CultureInfo.InvariantCulture));
    }
}
