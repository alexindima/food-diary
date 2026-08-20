using System.Net;
using System.Net.Sockets;

namespace FoodDiary.MailInbox.Infrastructure.Options;

internal sealed class MailInboxNetworkRange(byte[] networkBytes, int prefixLength) {
    public static bool IsValid(string value) => TryParse(value, out _);

    public static MailInboxNetworkRange Parse(string value) =>
        TryParse(value, out MailInboxNetworkRange? range)
            ? range!
            : throw new FormatException($"'{value}' is not a valid IP network.");

    public bool Contains(IPAddress address) {
        IPAddress normalized = Normalize(address);
        byte[] addressBytes = normalized.GetAddressBytes();
        if (addressBytes.Length != networkBytes.Length) {
            return false;
        }

        int wholeBytes = prefixLength / 8;
        int remainingBits = prefixLength % 8;
        for (int index = 0; index < wholeBytes; index++) {
            if (addressBytes[index] != networkBytes[index]) {
                return false;
            }
        }

        if (remainingBits == 0) {
            return true;
        }

        int mask = 0xff << (8 - remainingBits);
        return (addressBytes[wholeBytes] & mask) == (networkBytes[wholeBytes] & mask);
    }

    private static bool TryParse(string? value, out MailInboxNetworkRange? range) {
        range = null;
        if (string.IsNullOrWhiteSpace(value)) {
            return false;
        }

        string[] parts = value.Split('/', StringSplitOptions.TrimEntries);
        if (parts.Length is < 1 or > 2 || !IPAddress.TryParse(parts[0], out IPAddress? address)) {
            return false;
        }

        IPAddress normalized = Normalize(address);
        int maxPrefixLength = normalized.AddressFamily == AddressFamily.InterNetwork ? 32 : 128;
        int parsedPrefixLength = maxPrefixLength;
        if (parts.Length == 2 &&
            (!int.TryParse(parts[1], System.Globalization.CultureInfo.InvariantCulture, out parsedPrefixLength) ||
             parsedPrefixLength is <= 0 ||
             parsedPrefixLength > maxPrefixLength)) {
            return false;
        }

        byte[] bytes = normalized.GetAddressBytes();
        ApplyMask(bytes, parsedPrefixLength);
        range = new MailInboxNetworkRange(bytes, parsedPrefixLength);
        return true;
    }

    private static IPAddress Normalize(IPAddress address) =>
        address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;

    private static void ApplyMask(byte[] bytes, int prefixLength) {
        int wholeBytes = prefixLength / 8;
        int remainingBits = prefixLength % 8;
        if (remainingBits != 0) {
            bytes[wholeBytes] &= (byte)(0xff << (8 - remainingBits));
            wholeBytes++;
        }

        Array.Clear(bytes, wholeBytes, bytes.Length - wholeBytes);
    }
}
