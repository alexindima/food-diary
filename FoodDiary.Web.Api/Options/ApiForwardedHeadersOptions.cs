using System.Net;

namespace FoodDiary.Web.Api.Options;

public sealed class ApiForwardedHeadersOptions {
    public const string SectionName = "ForwardedHeaders";

    public int ForwardLimit { get; init; } = 1;

    public string[] KnownProxies { get; init; } = [];

    public string[] KnownNetworks { get; init; } = [];

    public static bool HasValidForwardLimit(ApiForwardedHeadersOptions options) => options.ForwardLimit > 0;

    public static bool HasValidKnownProxies(ApiForwardedHeadersOptions options) =>
        options.KnownProxies.All(static proxy => IPAddress.TryParse(proxy, out _));

    public static bool HasValidKnownNetworks(ApiForwardedHeadersOptions options) =>
        options.KnownNetworks.All(IsValidCidrNotation);

    private static bool IsValidCidrNotation(string cidr) {
        if (string.IsNullOrWhiteSpace(cidr)) {
            return false;
        }

        var parts = cidr.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2) {
            return false;
        }

        if (!IPAddress.TryParse(parts[0], out var address)) {
            return false;
        }

        if (!int.TryParse(parts[1], out var prefixLength)) {
            return false;
        }

        var maxPrefixLength = address.AddressFamily switch {
            System.Net.Sockets.AddressFamily.InterNetwork => 32,
            System.Net.Sockets.AddressFamily.InterNetworkV6 => 128,
            _ => -1
        };

        return maxPrefixLength >= 0 && prefixLength is >= 0 && prefixLength <= maxPrefixLength;
    }
}
