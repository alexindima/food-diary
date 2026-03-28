using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

namespace FoodDiary.Web.Api.Options;

public sealed class ForwardedHeadersOptionsSetup(IOptions<ApiForwardedHeadersOptions> apiForwardedHeadersOptions)
    : IConfigureOptions<ForwardedHeadersOptions> {
    public void Configure(ForwardedHeadersOptions options) {
        var settings = apiForwardedHeadersOptions.Value;

        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = settings.ForwardLimit;
        options.KnownProxies.Clear();
        options.KnownIPNetworks.Clear();

        foreach (var proxy in settings.KnownProxies) {
            options.KnownProxies.Add(IPAddress.Parse(proxy));
        }

        foreach (var network in settings.KnownNetworks) {
            options.KnownIPNetworks.Add(ParseNetwork(network));
        }
    }

    private static System.Net.IPNetwork ParseNetwork(string cidr) {
        var parts = cidr.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return new System.Net.IPNetwork(IPAddress.Parse(parts[0]), int.Parse(parts[1]));
    }
}
