using System.Net;

namespace FoodDiary.Application.Notifications.Common;

internal static class WebPushEndpointPolicy {
    public static bool IsAllowed(string endpoint) {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out Uri? uri) ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(uri.Host) ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Fragment) ||
            (!uri.IsDefaultPort && uri.Port != 443)) {
            return false;
        }

        string host = uri.DnsSafeHost;
        return Uri.CheckHostName(host) == UriHostNameType.Dns &&
               !IPAddress.TryParse(host, out _) &&
               !string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) &&
               !host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase) &&
               !host.EndsWith(".local", StringComparison.OrdinalIgnoreCase);
    }
}
