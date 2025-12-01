using System.Net.Mime;
using Amazon.S3;
using Amazon.S3.Model;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace FoodDiary.Infrastructure.Services;

public sealed class S3ImageStorageService(
    IAmazonS3 s3Client,
    IOptions<S3Options> options) : IImageStorageService
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        MediaTypeNames.Image.Jpeg,
        "image/png",
        "image/webp",
        "image/gif"
    };

    private readonly S3Options _options = options.Value;

    public Task<PresignedUpload> CreatePresignedUploadAsync(
        UserId userId,
        string fileName,
        string contentType,
        long fileSizeBytes,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        if (fileSizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fileSizeBytes), "File size must be greater than zero.");
        }

        if (fileSizeBytes > _options.MaxUploadSizeBytes)
        {
            throw new InvalidOperationException(
                $"File is too large. Max allowed size: {_options.MaxUploadSizeBytes} bytes.");
        }

        if (!AllowedContentTypes.Contains(contentType))
        {
            throw new InvalidOperationException($"Unsupported content type: {contentType}.");
        }

        var normalizedName = NormalizeFileName(fileName);
        var key = $"users/{userId.Value:D}/images/{Guid.NewGuid():N}-{normalizedName}";

        var expiresAt = DateTime.UtcNow.AddMinutes(15);
        var presignedRequest = new GetPreSignedUrlRequest
        {
            BucketName = _options.Bucket,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = expiresAt,
            ContentType = contentType
        };

        var uploadUrl = s3Client.GetPreSignedURL(presignedRequest);
        var fileUrl = BuildPublicUrl(key);

        var result = new PresignedUpload(uploadUrl, fileUrl, key, expiresAt);
        return Task.FromResult(result);
    }

    public async Task DeleteAsync(string objectKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
        {
            return;
        }

        var request = new DeleteObjectRequest
        {
            BucketName = _options.Bucket,
            Key = objectKey
        };

        await s3Client.DeleteObjectAsync(request, cancellationToken);
    }

    private static string NormalizeFileName(string fileName)
    {
        var nameOnly = Path.GetFileName(fileName);
        var cleaned = nameOnly.Replace(' ', '-');
        return cleaned.Length switch
        {
            0 => "image",
            > 128 => cleaned[..128],
            _ => cleaned
        };
    }

    private string BuildPublicUrl(string key)
    {
        if (!string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
        {
            return $"{_options.PublicBaseUrl.TrimEnd('/')}/{key}";
        }

        if (!string.IsNullOrWhiteSpace(_options.ServiceUrl))
        {
            return $"{_options.ServiceUrl!.TrimEnd('/')}/{_options.Bucket}/{key}";
        }

        return $"https://{_options.Bucket}.s3.{_options.Region}.amazonaws.com/{key}";
    }
}
