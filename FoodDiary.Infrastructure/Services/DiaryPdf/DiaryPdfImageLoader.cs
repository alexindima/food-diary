using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.ValueObjects.Ids;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using ImageSharpImage = SixLabors.ImageSharp.Image;
using ImageSharpRgbaImage = SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>;
using ImageSharpSize = SixLabors.ImageSharp.Size;

namespace FoodDiary.Infrastructure.Services.DiaryPdf;

internal sealed partial class DiaryPdfGenerator {
    private async Task<IReadOnlyDictionary<MealId, byte[]>> LoadMealImagesAsync(
        IReadOnlyList<Meal> meals,
        CancellationToken cancellationToken) {
        var result = new Dictionary<MealId, byte[]>();
        var cache = new Dictionary<string, byte[]?>(StringComparer.Ordinal);

        foreach (var meal in meals.Take(60)) {
            var image = await LoadMealImageForReportAsync(meal, cache, cancellationToken);
            if (image is not null) {
                result[meal.Id] = image;
            }
        }

        return result;
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
        Dictionary<string, byte[]?> cache,
        CancellationToken cancellationToken) {
        if (!string.IsNullOrWhiteSpace(meal.ImageUrl)) {
            return await LoadCachedMealImageAsync(meal.ImageUrl, cache, cancellationToken);
        }

        var ingredientImages = new List<byte[]>();
        foreach (var imageUrl in GetIngredientImageUrls(meal).Take(4)) {
            var image = await LoadCachedMealImageAsync(imageUrl, cache, cancellationToken);
            if (image is not null) {
                ingredientImages.Add(image);
            }
        }

        return CreateMealImageCollage(ingredientImages);
    }

    private async Task<byte[]?> LoadCachedMealImageAsync(
        string imageUrl,
        Dictionary<string, byte[]?> cache,
        CancellationToken cancellationToken) {
        if (cache.TryGetValue(imageUrl, out var cached)) {
            return cached;
        }

        var image = await LoadMealImageAsync(imageUrl, cancellationToken);
        cache[imageUrl] = image;
        return image;
    }

    private async Task<byte[]?> LoadMealImageAsync(string imageUrl, CancellationToken cancellationToken) {
        try {
            if (TryReadDataUrl(imageUrl, out var dataUrlBytes)) {
                return PrepareMealImage(dataUrlBytes);
            }

            if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) ||
                uri.Scheme is not ("http" or "https")) {
                return null;
            }

            using var response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength > MaxMealImageBytes) {
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var memory = new MemoryStream();
            var buffer = new byte[81920];
            int read;

            while ((read = await stream.ReadAsync(buffer, cancellationToken)) > 0) {
                memory.Write(buffer, 0, read);
                if (memory.Length > MaxMealImageBytes) {
                    return null;
                }
            }

            return memory.Length == 0 ? null : PrepareMealImage(memory.ToArray());
        } catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or TaskCanceledException or FormatException) {
            return null;
        }
    }

    private static IReadOnlyList<string> GetIngredientImageUrls(Meal meal) =>
        meal.Items
            .OrderBy(item => item.CreatedOnUtc)
            .Select(item => item.Product?.ImageUrl ?? item.Recipe?.ImageUrl)
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

        bytes = Convert.FromBase64String(value[(markerIndex + marker.Length)..]);
        return bytes.Length <= MaxMealImageBytes;
    }

    private static byte[]? PrepareMealImage(byte[] imageBytes) {
        try {
            using var image = ImageSharpImage.Load(imageBytes);
            image.Mutate(context => context.Resize(new ResizeOptions {
                Size = new ImageSharpSize(MealImageThumbnailSize, MealImageThumbnailSize),
                Mode = ResizeMode.Crop,
                Position = AnchorPositionMode.Center
            }));

            using var output = new MemoryStream();
            image.Save(output, new JpegEncoder { Quality = 86 });
            return output.ToArray();
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
            using var canvas = new ImageSharpRgbaImage(
                MealImageThumbnailSize,
                MealImageThumbnailSize,
                new Rgba32(27, 34, 43));
            var slots = GetCollageSlots(images.Count);

            for (var index = 0; index < Math.Min(images.Count, slots.Length); index++) {
                using var tile = ImageSharpImage.Load<Rgba32>(images[index]);
                var slot = slots[index];
                tile.Mutate(context => context.Resize(new ResizeOptions {
                    Size = new ImageSharpSize(slot.Width, slot.Height),
                    Mode = ResizeMode.Crop,
                    Position = AnchorPositionMode.Center
                }));

                CopyImage(tile, canvas, slot.X, slot.Y);
            }

            using var output = new MemoryStream();
            canvas.Save(output, new JpegEncoder { Quality = 86 });
            return output.ToArray();
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

    private static void CopyImage(ImageSharpRgbaImage source, ImageSharpRgbaImage target, int targetX, int targetY) {
        for (var y = 0; y < source.Height; y++) {
            var sourceRow = source.DangerousGetPixelRowMemory(y).Span;
            var targetRow = target.DangerousGetPixelRowMemory(targetY + y).Span[targetX..(targetX + source.Width)];
            sourceRow.CopyTo(targetRow);
        }
    }
    private readonly record struct CollageSlot(int X, int Y, int Width, int Height);
}
