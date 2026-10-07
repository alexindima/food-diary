using System.Globalization;
using System.Net.Http.Headers;

namespace FoodDiary.Telegram.Bot.Api;

/// <summary>Keep request credentials local to one call and delegate HTTP policy to the named client.</summary>
internal sealed class BotApiTransport(HttpClient client, Uri baseUri) {
    internal HttpRequestMessage CreateRequest(string method, string path, BotApiRequestContext context) {
        var request = new HttpRequestMessage(new HttpMethod(method), new Uri(baseUri, path));
        if (context.AccessToken is not null) {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
        }
        if (context.ApiSecret is not null) {
            request.Headers.Add("X-Telegram-Bot-Secret", context.ApiSecret);
        }
        return request;
    }

    internal Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

    internal static string PathValue(object value) => Uri.EscapeDataString(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);

    internal static string AddQuery(string path, string name, object? value) => value is null
        ? path
        : path + (path.Contains('?', StringComparison.Ordinal) ? "&" : "?") + Uri.EscapeDataString(char.ToLowerInvariant(name[0]) + name[1..]) + "=" + PathValue(value);

    internal static void SetHeader(HttpRequestMessage request, string name, object? value) {
        if (value is not null) {
            request.Headers.Add(name, Convert.ToString(value, CultureInfo.InvariantCulture));
        }
    }
}
