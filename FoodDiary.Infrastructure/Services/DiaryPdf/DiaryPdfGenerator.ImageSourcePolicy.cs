using System.Net;
using System.Net.Sockets;

namespace FoodDiary.Infrastructure.Services.DiaryPdf;

internal sealed partial class DiaryPdfGenerator {
    private const int MaxDataUrlBase64Length = ((MaxMealImageBytes + 2) / 3) * 4;

    internal static Func<string, CancellationToken, Task<IPAddress[]>> RemoteImageHostResolver { get; set; } =
        static (host, cancellationToken) => Dns.GetHostAddressesAsync(host, cancellationToken);

    private static bool TryReadDataUrl(string value, out byte[] bytes) {
        bytes = [];
        const string marker = ";base64,";
        int markerIndex = value.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (!value.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase) || markerIndex < 0) {
            return false;
        }

        string base64 = value[(markerIndex + marker.Length)..];
        if (base64.Length > MaxDataUrlBase64Length) {
            return false;
        }

        bytes = Convert.FromBase64String(base64);
        return bytes.Length <= MaxMealImageBytes;
    }

    private static async Task<bool> IsAllowedRemoteImageUriAsync(Uri uri, CancellationToken cancellationToken) {
        if (uri.Scheme is not ("http" or "https") || string.IsNullOrWhiteSpace(uri.Host)) {
            return false;
        }

        string host = uri.IdnHost;
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase)) {
            return false;
        }

        if (IPAddress.TryParse(host, out IPAddress? literalAddress)) {
            return IsPublicAddress(literalAddress);
        }

        try {
            IPAddress[] addresses = await RemoteImageHostResolver(host, cancellationToken).ConfigureAwait(false);
            return addresses.Length > 0 && addresses.All(IsPublicAddress);
        } catch (SocketException) {
            return false;
        }
    }

    private static bool IsPublicAddress(IPAddress address) => RemoteImageAddressPolicy.IsPublicAddress(address);
}
