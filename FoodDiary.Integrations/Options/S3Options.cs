namespace FoodDiary.Integrations.Options;

public sealed class S3Options {
    public const string SectionName = "S3";
    public const long MaximumUploadSizeBytes = 50 * 1024 * 1024;

    /// <summary>
    /// AWS access key ID.
    /// </summary>
    public string AccessKeyId { get; init; } = string.Empty;

    /// <summary>
    /// AWS secret access key.
    /// </summary>
    public string SecretAccessKey { get; init; } = string.Empty;

    /// <summary>
    /// Default region (e.g. eu-central-1).
    /// </summary>
    public string Region { get; init; } = string.Empty;

    /// <summary>
    /// Target bucket name.
    /// </summary>
    public string Bucket { get; init; } = string.Empty;

    /// <summary>
    /// Optional custom endpoint for S3-compatible storage (e.g. MinIO).
    /// </summary>
    public string? ServiceUrl { get; init; }

    /// <summary>
    /// Optional CDN or CloudFront base URL used for public access if different from the bucket endpoint.
    /// </summary>
    public string? PublicBaseUrl { get; init; }

    /// <summary>
    /// Upload size limit in bytes.
    /// </summary>
    public long MaxUploadSizeBytes { get; init; } = 20 * 1024 * 1024; // 20 MB

    public static bool HasValidMaxUploadSize(S3Options options) =>
        options.MaxUploadSizeBytes is > 0 and <= MaximumUploadSizeBytes;

    public static bool IsEmptyOrComplete(S3Options options) {
        return !HasAnyConfiguration(options) || HasCompleteConfiguration(options);
    }

    public static bool HasCompleteConfiguration(S3Options options) =>
        !string.IsNullOrWhiteSpace(options.AccessKeyId) &&
        !string.IsNullOrWhiteSpace(options.SecretAccessKey) &&
        !string.IsNullOrWhiteSpace(options.Bucket) &&
        (!string.IsNullOrWhiteSpace(options.Region) || !string.IsNullOrWhiteSpace(options.ServiceUrl));

    public static bool HasValidPublicBaseUrl(S3Options options) {
        return string.IsNullOrWhiteSpace(options.PublicBaseUrl) ||
               IntegrationUriValidator.IsAbsoluteHttpBaseUrl(options.PublicBaseUrl);
    }

    public static bool HasValidServiceUrl(S3Options options) {
        return string.IsNullOrWhiteSpace(options.ServiceUrl) ||
               IntegrationUriValidator.IsAbsoluteHttpBaseUrl(options.ServiceUrl);
    }

    private static bool HasAnyConfiguration(S3Options options) =>
        !string.IsNullOrWhiteSpace(options.AccessKeyId) ||
        !string.IsNullOrWhiteSpace(options.SecretAccessKey) ||
        !string.IsNullOrWhiteSpace(options.Region) ||
        !string.IsNullOrWhiteSpace(options.Bucket) ||
        !string.IsNullOrWhiteSpace(options.ServiceUrl) ||
        !string.IsNullOrWhiteSpace(options.PublicBaseUrl);
}
