using System.Net;
using System.Net.Sockets;

namespace FoodDiary.Infrastructure.Services.DiaryPdf;

internal sealed partial class DiaryPdfGenerator {
    private const int MaxDataUrlBase64Length = ((MaxMealImageBytes + 2) / 3) * 4;

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
            IPAddress[] addresses = await Dns.GetHostAddressesAsync(host, cancellationToken).ConfigureAwait(false);
            return addresses.Length > 0 && addresses.All(IsPublicAddress);
        } catch (SocketException) {
            return false;
        }
    }

    private static bool IsPublicAddress(IPAddress address) {
        if (address.IsIPv4MappedToIPv6) {
            address = address.MapToIPv4();
        }

        if (IPAddress.IsLoopback(address)) {
            return false;
        }

        byte[] bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetwork) {
            return bytes[0] is not (0 or 10 or 127) &&
                   (bytes[0] != 100 || bytes[1] < 64 || bytes[1] > 127) &&
                   (bytes[0] != 169 || bytes[1] != 254) &&
                   (bytes[0] != 172 || bytes[1] < 16 || bytes[1] > 31) &&
                   (bytes[0] != 192 || bytes[1] != 0 || bytes[2] != 0) &&
                   (bytes[0] != 192 || bytes[1] != 168) &&
                   (bytes[0] != 198 || bytes[1] is not (18 or 19)) &&
                   bytes[0] < 224;
        }

        // A parsed or DNS-resolved address is always IPv4 or IPv6, so this handles IPv6.
        return !address.IsIPv6LinkLocal &&
               !address.IsIPv6Multicast &&
               !address.IsIPv6SiteLocal &&
               !address.Equals(IPAddress.IPv6Any) &&
               !address.Equals(IPAddress.IPv6Loopback) &&
               (bytes[0] & 0xfe) != 0xfc;
    }
}
