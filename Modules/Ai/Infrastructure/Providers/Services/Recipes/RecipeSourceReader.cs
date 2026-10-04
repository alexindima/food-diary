using System.Net;
using System.Net.Sockets;
using FoodDiary.Integrations.Http.Http;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Results;

namespace FoodDiary.Modules.Ai.Infrastructure.Providers.Services.Recipes;

public sealed class RecipeSourceReader(HttpClient httpClient) : IRecipeSourceReader {
    public async Task<Result<RecipeSource>> ReadAsync(string sourceUrl, CancellationToken cancellationToken) {
        if (!TryNormalizeUrl(sourceUrl, out Uri? url)) {
            return Result.Failure<RecipeSource>(AiErrors.InvalidRecipeUrl());
        }
        try {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(20));
            for (int redirect = 0; redirect <= 3; redirect++) {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.UserAgent.ParseAdd("FoodDiary-RecipeImport/1.0");
                request.Headers.Accept.ParseAdd("text/html,application/ld+json,application/json,text/plain");
                using HttpResponseMessage response = await httpClient.SendAsync(
                    request, HttpCompletionOption.ResponseHeadersRead, deadline.Token).ConfigureAwait(false);
                if ((int)response.StatusCode is >= 300 and <= 399) {
                    Uri? target = response.Headers.Location;
                    if (target is null || !TryNormalizeUrl(new Uri(url!, target).AbsoluteUri, out url)) {
                        return Result.Failure<RecipeSource>(AiErrors.RecipeSourceUnavailable());
                    }
                    continue;
                }
                string? mediaType = response.Content.Headers.ContentType?.MediaType;
                if (!response.IsSuccessStatusCode || mediaType is not ("text/html" or "text/plain" or "application/json" or "application/ld+json")) {
                    return Result.Failure<RecipeSource>(AiErrors.RecipeSourceUnavailable());
                }
                string body = await BoundedHttpContentReader.ReadAsStringAsync(
                    response.Content, 1024 * 1024, TimeSpan.FromSeconds(10), deadline.Token).ConfigureAwait(false);
                string extracted = string.Equals(mediaType, "text/plain", StringComparison.Ordinal) ? body.Trim() : RecipeSourceTextExtractor.Extract(body);
                if (string.IsNullOrWhiteSpace(extracted)) {
                    return Result.Failure<RecipeSource>(AiErrors.RecipeSourceUnavailable());
                }
                return Result.Success(new RecipeSource(extracted[..Math.Min(extracted.Length, 16000)], url!.AbsoluteUri));
            }
        } catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidDataException or TimeoutException or UriFormatException) {
            return Result.Failure<RecipeSource>(AiErrors.RecipeSourceUnavailable());
        } catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
            return Result.Failure<RecipeSource>(AiErrors.RecipeSourceUnavailable());
        }
        return Result.Failure<RecipeSource>(AiErrors.RecipeSourceUnavailable());
    }

    public static bool TryNormalizeUrl(string source, out Uri? url) {
        if (!Uri.TryCreate(source.Trim(), UriKind.Absolute, out url) || !IsAllowedUrl(url)) {
            return false;
        }
        if (url.Host.Equals("vk.ru", StringComparison.OrdinalIgnoreCase) && string.Equals(url.AbsolutePath, "/away.php", StringComparison.Ordinal)) {
            string? target = url.Query.TrimStart('?').Split('&')
                .FirstOrDefault(x => x.StartsWith("to=", StringComparison.Ordinal))?[3..];
            if (target is null || !Uri.TryCreate(Uri.UnescapeDataString(target), UriKind.Absolute, out url) || !IsAllowedUrl(url)) {
                return false;
            }
        }
        if (url.Host.Equals("instagram.com", StringComparison.OrdinalIgnoreCase) ||
            url.Host.Equals("www.instagram.com", StringComparison.OrdinalIgnoreCase)) {
            url = new UriBuilder(url) { Query = string.Empty, Fragment = string.Empty }.Uri;
        }
        return true;
    }

    private static bool IsAllowedUrl(Uri url) => string.Equals(url.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal) && url.Port == 443 &&
        string.IsNullOrEmpty(url.UserInfo) && !string.IsNullOrEmpty(url.Host) && url.AbsoluteUri.Length <= 1400;

    // DNS is checked at connection time and the validated address is used for the actual socket,
    // preventing redirects or DNS rebinding from reaching private infrastructure.
    public static async ValueTask<Stream> ConnectPublicAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken) {
        IPAddress[] addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, cancellationToken).ConfigureAwait(false);
        if (addresses.Length == 0 || addresses.Any(x => !IsPublicAddress(x))) {
            throw new HttpRequestException("Recipe source is not a public network destination.");
        }
        foreach (IPAddress address in addresses) {
            var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try {
                await socket.ConnectAsync(new IPEndPoint(address, context.DnsEndPoint.Port), cancellationToken).ConfigureAwait(false);
                return new NetworkStream(socket, ownsSocket: true);
            } catch (SocketException) {
                socket.Dispose();
            } catch {
                socket.Dispose();
                throw;
            }
        }
        throw new HttpRequestException("Recipe source connection failed.");
    }

    public static bool IsPublicAddress(IPAddress address) {
        if (address.IsIPv4MappedToIPv6) {
            address = address.MapToIPv4();
        }
        byte[] bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetwork) {
            return bytes[0] is not (0 or 10 or 127) && bytes[0] < 224 &&
                !(bytes[0] == 100 && bytes[1] is >= 64 and <= 127) &&
                !(bytes[0] == 169 && bytes[1] == 254) &&
                !(bytes[0] == 172 && bytes[1] is >= 16 and <= 31) &&
                !(bytes[0] == 192 && ((bytes[1] == 0 && bytes[2] is 0 or 2) || bytes[1] == 168 || (bytes[1] == 88 && bytes[2] == 99))) &&
                !(bytes[0] == 198 && (bytes[1] is 18 or 19 || (bytes[1] == 51 && bytes[2] == 100))) &&
                !(bytes[0] == 203 && bytes[1] == 0 && bytes[2] == 113);
        }
        return address.AddressFamily == AddressFamily.InterNetworkV6 && (bytes[0] & 0xe0) == 0x20 &&
            !(bytes[0] == 0x20 && bytes[1] == 0x02) &&
            !(bytes[0] == 0x20 && bytes[1] == 0x01 && (bytes[2] < 2 || (bytes[2] == 0x0d && bytes[3] == 0xb8))) &&
            !(bytes[0] == 0x3f && bytes[1] == 0xff);
    }
}
