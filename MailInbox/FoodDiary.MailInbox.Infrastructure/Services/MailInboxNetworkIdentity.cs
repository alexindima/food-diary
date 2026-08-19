using System.Net;

namespace FoodDiary.MailInbox.Infrastructure.Services;

internal static class MailInboxNetworkIdentity {
    private const int Ipv6PrefixBytes = 8;

    public static string GetKey(IPAddress address) {
        IPAddress normalized = address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
        if (normalized.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6) {
            return normalized.ToString();
        }

        byte[] bytes = normalized.GetAddressBytes();
        Array.Clear(bytes, Ipv6PrefixBytes, bytes.Length - Ipv6PrefixBytes);
        return new IPAddress(bytes).ToString();
    }
}
