using FoodDiary.Integrations.Http.Http;

namespace FoodDiary.Modules.Ai.Infrastructure.Providers.Services.Recipes;

internal sealed class RecipeYouTubeHttpHandler : DelegatingHandler {
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        HttpRequestMessage current = request;
        try {
            for (int hop = 0; hop <= 3; hop++) {
                HttpResponseMessage response = await SendAllowedAsync(current, cancellationToken).ConfigureAwait(false);
                if ((int)response.StatusCode is not (301 or 302 or 303 or 307 or 308)) {
                    return response;
                }
                using (response) {
                    if (response.Headers.Location is null || hop == 3) {
                        throw new HttpRequestException("YouTube source redirect limit exceeded.");
                    }
                    Uri target = new(current.RequestUri!, response.Headers.Location);
                    bool keepBody = (int)response.StatusCode is 307 or 308;
                    var next = new HttpRequestMessage(keepBody ? current.Method : HttpMethod.Get, target);
                    foreach (string name in new[] { "User-Agent", "Accept", "Range" }) {
                        if (current.Headers.TryGetValues(name, out IEnumerable<string>? values)) {
                            next.Headers.TryAddWithoutValidation(name, values);
                        }
                    }
                    if (keepBody && current.Content is not null) {
                        next.Content = new ByteArrayContent(await BoundedHttpContentReader.ReadAsByteArrayAsync(
                            current.Content, 64 * 1024, TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false));
                        next.Content.Headers.ContentType = current.Content.Headers.ContentType;
                    }
                    if (!ReferenceEquals(current, request)) {
                        current.Dispose();
                    }
                    current = next;
                }
            }
            throw new HttpRequestException("YouTube source redirect limit exceeded.");
        } finally {
            if (!ReferenceEquals(current, request)) {
                current.Dispose();
            }
        }
    }

    private async Task<HttpResponseMessage> SendAllowedAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        Uri? url = request.RequestUri;
        if (url is null || !string.Equals(url.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal) || url.Port != 443 ||
            url.UserInfo.Length != 0 || !IsAllowedHost(url.IdnHost)) {
            throw new HttpRequestException("Unsupported YouTube network destination.");
        }
        HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (IsMediaHost(url.IdnHost)) {
            // Media is streamed in bounded segments and copied through the shared 50 MiB limit.
            return response;
        }
        try {
            HttpContent original = response.Content;
            byte[] body = await BoundedHttpContentReader.ReadAsByteArrayAsync(
                original, 4 * 1024 * 1024, TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
            var buffered = new ByteArrayContent(body);
            foreach (KeyValuePair<string, IEnumerable<string>> header in original.Headers) {
                buffered.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
            response.Content = buffered;
            original.Dispose();
            return response;
        } catch (InvalidDataException) {
            response.Dispose();
            throw new HttpRequestException("YouTube response exceeded the supported limit.");
        } catch {
            response.Dispose();
            throw;
        }
    }

    private static bool IsAllowedHost(string host) => host is "www.youtube.com" or "youtube.com" or "youtubei.googleapis.com" || IsMediaHost(host);
    private static bool IsMediaHost(string host) => host.EndsWith(".googlevideo.com", StringComparison.Ordinal);
}
