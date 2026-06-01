using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.ValueObjects.Ids;
using SkiaSharp;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace FoodDiary.Infrastructure.Services.DiaryPdf;

internal sealed partial class DiaryPdfGenerator {
    private const int MaxDataUrlBase64Length = ((MaxMealImageBytes + 2) / 3) * 4;

    private async Task<IReadOnlyDictionary<MealId, byte[]>> LoadMealImagesAsync(
        IReadOnlyList<Meal> meals,
        CancellationToken cancellationToken) {
        using var gate = new SemaphoreSlim(MaxParallelMealImageDownloads);
        var cache = new Dictionary<string, Lazy<Task<byte[]?>>>(StringComparer.Ordinal);
        var tasks = meals
            .Take(MaxMealImagesPerReport)
            .Select(meal => LoadMealImageEntryAsync(meal, cache, gate, cancellationToken))
            .ToArray();

        var entries = await Task.WhenAll(tasks).ConfigureAwait(false);

        return entries
            .Where(entry => entry.Image is not null)
            .ToDictionary(entry => entry.MealId, entry => entry.Image!, EqualityComparer<MealId>.Default);
    }

    private async Task<MealImageEntry> LoadMealImageEntryAsync(
        Meal meal,
        Dictionary<string, Lazy<Task<byte[]?>>> cache,
        SemaphoreSlim gate,
        CancellationToken cancellationToken) {
        var image = await LoadMealImageForReportAsync(meal, cache, gate, cancellationToken).ConfigureAwait(false);
        return new MealImageEntry(meal.Id, image);
    }

    private static bool ShouldUseCompactMealsMode(DateTime dateFrom, DateTime dateTo) =>
        GetReportDayCount(dateFrom, dateTo) > 7;

    private static int GetReportDayCount(DateTime dateFrom, DateTime dateTo) {
        var normalizedFrom = EnsureUtcForReport(dateFrom);
        var normalizedTo = EnsureUtcForReport(dateTo);
        if (normalizedTo < normalizedFrom) {
            (normalizedFrom, normalizedTo) = (normalizedTo, normalizedFrom);
        }

        var duration = normalizedTo - normalizedFrom;
        return Math.Clamp((int)Math.Ceiling(duration.TotalDays), 1, 366);
    }

    private static DateTime EnsureUtcForReport(DateTime value) =>
        value.Kind switch {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };

    private async Task<byte[]?> LoadMealImageForReportAsync(
        Meal meal,
        Dictionary<string, Lazy<Task<byte[]?>>> cache,
        SemaphoreSlim gate,
        CancellationToken cancellationToken) {
        if (!string.IsNullOrWhiteSpace(meal.ImageUrl)) {
            return await LoadCachedMealImageAsync(meal.ImageUrl, cache, gate, cancellationToken).ConfigureAwait(false);
        }

        var compositionImages = await Task.WhenAll(
            GetCompositionImageUrls(meal)
                .Take(MaxIngredientImagesPerCollage)
                .Select(imageUrl => LoadCachedMealImageAsync(imageUrl, cache, gate, cancellationToken))).ConfigureAwait(false);

        return CreateMealImageCollage(compositionImages.Where(image => image is not null).Cast<byte[]>().ToArray());
    }

    private async Task<byte[]?> LoadCachedMealImageAsync(
        string imageUrl,
        Dictionary<string, Lazy<Task<byte[]?>>> cache,
        SemaphoreSlim gate,
        CancellationToken cancellationToken) {
        Lazy<Task<byte[]?>> cached;
        lock (cache) {
            if (!cache.TryGetValue(imageUrl, out cached!)) {
                cached = new Lazy<Task<byte[]?>>(
                    () => LoadMealImageWithGateAsync(imageUrl, gate, cancellationToken),
                    LazyThreadSafetyMode.ExecutionAndPublication);
                cache[imageUrl] = cached;
            }
        }

        return await cached.Value.ConfigureAwait(false);
    }

    private async Task<byte[]?> LoadMealImageWithGateAsync(
        string imageUrl,
        SemaphoreSlim gate,
        CancellationToken cancellationToken) {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try {
            return await LoadMealImageAsync(imageUrl, cancellationToken).ConfigureAwait(false);
        } finally {
            gate.Release();
        }
    }

    private async Task<byte[]?> LoadMealImageAsync(string imageUrl, CancellationToken cancellationToken) {
        try {
            if (TryReadDataUrl(imageUrl, out var dataUrlBytes)) {
                return PrepareMealImage(dataUrlBytes);
            }

            if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) ||
                !await IsAllowedRemoteImageUriAsync(uri, cancellationToken).ConfigureAwait(false)) {
                return null;
            }

            using var response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength > MaxMealImageBytes) {
                return null;
            }

            var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using (stream.ConfigureAwait(false)) {
                using var memory = new MemoryStream();
                var buffer = new byte[81920];
                int read;

                while ((read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0) {
                    memory.Write(buffer, 0, read);
                    if (memory.Length > MaxMealImageBytes) {
                        return null;
                    }
                }

                return memory.Length == 0 ? null : PrepareMealImage(memory.ToArray());
            }
        } catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or TaskCanceledException or FormatException or IOException or SocketException) {
            return null;
        }
    }

    [StructLayout(LayoutKind.Auto)]
    private readonly record struct MealImageEntry(MealId MealId, byte[]? Image);

    private static IReadOnlyList<string> GetCompositionImageUrls(Meal meal) =>
        meal.Items
            .OrderBy(item => item.CreatedOnUtc)
            .Select(item => item.Product?.ImageUrl ?? item.Recipe?.ImageUrl)
            .Concat(meal.AiSessions
                .OrderBy(session => session.RecognizedAtUtc)
                .Select(session => session.ImageAsset?.Url))
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Distinct(StringComparer.Ordinal)
            .Cast<string>()
            .ToArray();

    private static bool TryReadDataUrl(string value, out byte[] bytes) {
        bytes = [];
        const string marker = ";base64,";
        var markerIndex = value.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (!value.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase) || markerIndex < 0) {
            return false;
        }

        var base64 = value[(markerIndex + marker.Length)..];
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

        var host = uri.IdnHost;
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase)) {
            return false;
        }

        if (IPAddress.TryParse(host, out var literalAddress)) {
            return IsPublicAddress(literalAddress);
        }

        var addresses = await Dns.GetHostAddressesAsync(host, cancellationToken).ConfigureAwait(false);
        return addresses.Length > 0 && addresses.All(IsPublicAddress);
    }

    private static bool IsPublicAddress(IPAddress address) {
        if (address.IsIPv4MappedToIPv6) {
            address = address.MapToIPv4();
        }

        if (IPAddress.IsLoopback(address)) {
            return false;
        }

        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) {
            var bytes = address.GetAddressBytes();
            return bytes[0] is not (0 or 10 or 127) &&
                   (bytes[0] != 100 || bytes[1] < 64 || bytes[1] > 127) &&
                   (bytes[0] != 169 || bytes[1] != 254) &&
                   (bytes[0] != 172 || bytes[1] < 16 || bytes[1] > 31) &&
                   (bytes[0] != 192 || bytes[1] != 0 || bytes[2] != 0) &&
                   (bytes[0] != 192 || bytes[1] != 168) &&
                   (bytes[0] != 198 || bytes[1] is not (18 or 19)) &&
                   bytes[0] < 224;
        }

        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6) {
            var bytes = address.GetAddressBytes();
            return !address.IsIPv6LinkLocal &&
                   !address.IsIPv6Multicast &&
                   !address.IsIPv6SiteLocal &&
                   !address.Equals(IPAddress.IPv6Any) &&
                   !address.Equals(IPAddress.IPv6Loopback) &&
                   (bytes[0] & 0xfe) != 0xfc;
        }

        return false;
    }

    private static byte[]? PrepareMealImage(byte[] imageBytes) {
        try {
            using var image = SKBitmap.Decode(imageBytes);
            if (image is null) {
                return null;
            }

            using var preparedImage = CreateCroppedBitmap(image, MealImageThumbnailSize, MealImageThumbnailSize);
            return EncodeMealImage(preparedImage);
        } catch {
            return null;
        }
    }

    private static byte[]? CreateMealImageCollage(IReadOnlyList<byte[]> images) {
        if (images.Count == 0) {
            return null;
        }

        if (images.Count == 1) {
            return images[0];
        }

        try {
            using var canvas = new SKBitmap(
                MealImageThumbnailSize,
                MealImageThumbnailSize,
                SKColorType.Rgba8888,
                SKAlphaType.Premul);
            using var skCanvas = new SKCanvas(canvas);
            skCanvas.Clear(new SKColor(27, 34, 43));
            var slots = GetCollageSlots(images.Count);

            for (var index = 0; index < Math.Min(images.Count, slots.Length); index++) {
                using var tile = SKBitmap.Decode(images[index]);
                if (tile is null) {
                    continue;
                }

                var slot = slots[index];
                using var resizedTile = CreateCroppedBitmap(tile, slot.Width, slot.Height);
                skCanvas.DrawBitmap(resizedTile, slot.X, slot.Y);
            }

            return EncodeMealImage(canvas);
        } catch {
            return null;
        }
    }

    private static CollageSlot[] GetCollageSlots(int imageCount) {
        var half = MealImageThumbnailSize / 2;
        return imageCount switch {
            2 => [
                new CollageSlot(0, 0, half, MealImageThumbnailSize),
                new CollageSlot(half, 0, half, MealImageThumbnailSize)
            ],
            3 => [
                new CollageSlot(0, 0, half, MealImageThumbnailSize),
                new CollageSlot(half, 0, half, half),
                new CollageSlot(half, half, half, half)
            ],
            _ => [
                new CollageSlot(0, 0, half, half),
                new CollageSlot(half, 0, half, half),
                new CollageSlot(0, half, half, half),
                new CollageSlot(half, half, half, half)
            ]
        };
    }

    private static SKBitmap CreateCroppedBitmap(SKBitmap source, int width, int height) {
        var scale = Math.Max((float)width / source.Width, (float)height / source.Height);
        var scaledWidth = source.Width * scale;
        var scaledHeight = source.Height * scale;
        var destination = new SKRect(
            (width - scaledWidth) / 2,
            (height - scaledHeight) / 2,
            (width + scaledWidth) / 2,
            (height + scaledHeight) / 2);
        var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);

        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        canvas.DrawBitmap(source, destination);

        return bitmap;
    }

    private static byte[] EncodeMealImage(SKBitmap image) {
        using var encoded = image.Encode(SKEncodedImageFormat.Jpeg, 86);
        return encoded.ToArray();
    }

    [StructLayout(LayoutKind.Auto)]
    private readonly record struct CollageSlot(int X, int Y, int Width, int Height);
}
