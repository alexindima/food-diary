using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;
using WebPush;

namespace FoodDiary.Integrations.Options;

public sealed class WebPushOptions {
    public const string SectionName = "WebPush";

    public bool Enabled { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
    public string DefaultUrl { get; set; } = "/";

    public static bool HasValidConfiguration(WebPushOptions options) {
        if (!options.Enabled) {
            return true;
        }

        return !string.IsNullOrWhiteSpace(options.Subject)
               && HasValidVapidKeys(options)
               && options.Subject.Length <= 256
               && IntegrationUriValidator.IsVapidSubject(options.Subject)
               && !string.IsNullOrWhiteSpace(options.DefaultUrl)
               && options.DefaultUrl.Length <= 256
               && IntegrationUriValidator.IsSafeNavigationUrl(options.DefaultUrl);
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private static bool HasValidVapidKeys(WebPushOptions options) {
        try {
            VapidHelper.ValidatePublicKey(options.PublicKey);
            VapidHelper.ValidatePrivateKey(options.PrivateKey);
            byte[] publicKey = WebEncoders.Base64UrlDecode(options.PublicKey);
            byte[] privateKey = WebEncoders.Base64UrlDecode(options.PrivateKey);
            if (publicKey[0] != 0x04) {
                return false;
            }

            using var signingKey = ECDsa.Create(new ECParameters {
                Curve = ECCurve.NamedCurves.nistP256,
                D = privateKey,
            });
            ECPoint derivedPublicKey = signingKey.ExportParameters(includePrivateParameters: false).Q;
            return derivedPublicKey.X is not null &&
                   derivedPublicKey.Y is not null &&
                   CryptographicOperations.FixedTimeEquals(publicKey.AsSpan(1, 32), derivedPublicKey.X) &&
                   CryptographicOperations.FixedTimeEquals(publicKey.AsSpan(33, 32), derivedPublicKey.Y);
        } catch (Exception exception) when (exception is ArgumentException or FormatException or CryptographicException) {
            return false;
        }
    }
}
