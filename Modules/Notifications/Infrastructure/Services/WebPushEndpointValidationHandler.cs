using System.Net;

namespace FoodDiary.Integrations.Services;

internal sealed class WebPushEndpointValidationHandler : DelegatingHandler {
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) {
        Uri? uri = request.RequestUri;
        if (uri?.IsAbsoluteUri != true ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(uri.Host) ||
            Uri.CheckHostName(uri.DnsSafeHost) != UriHostNameType.Dns ||
            IPAddress.TryParse(uri.DnsSafeHost, out _) ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Fragment) ||
            (!uri.IsDefaultPort && uri.Port != 443)) {
            throw new HttpRequestException("Web push endpoint must be a public HTTPS host on port 443.");
        }

        return base.SendAsync(request, cancellationToken);
    }
}
