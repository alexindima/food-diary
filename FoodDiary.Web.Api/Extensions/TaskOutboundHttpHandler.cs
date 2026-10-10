namespace FoodDiary.Web.Api.Extensions;

public sealed class TaskOutboundHttpHandler : DelegatingHandler {
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        if (request.RequestUri is not { IsAbsoluteUri: true, IsLoopback: true } uri ||
            (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.Ordinal) &&
             !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal))) {
            throw new HttpRequestException("External providers are disabled in the isolated task runtime.");
        }
        return base.SendAsync(request, cancellationToken);
    }
}
