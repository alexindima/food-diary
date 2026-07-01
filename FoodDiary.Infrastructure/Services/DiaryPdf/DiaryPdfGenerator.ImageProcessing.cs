using SkiaSharp;
using System.Runtime.InteropServices;

namespace FoodDiary.Infrastructure.Services.DiaryPdf;

internal sealed partial class DiaryPdfGenerator {
    private static readonly SKSamplingOptions MealImageSamplingOptions = new(SKFilterMode.Linear, SKMipmapMode.Linear);

    private static byte[]? PrepareMealImage(byte[] imageBytes) {
        try {
            using var image = SKBitmap.Decode(imageBytes);
            if (image is null) {
                return null;
            }

            using SKBitmap preparedImage = CreateCroppedBitmap(image, MealImageThumbnailSize, MealImageThumbnailSize);
            return EncodeMealImage(preparedImage);
        } catch {
            return null;
        }
    }

    private static byte[]? CreateMealImageCollage(IReadOnlyList<byte[]> images) {
        switch (images.Count) {
            case 0:
                return null;
            case 1:
                return images[0];
            default:
                try {
                    using var canvas = new SKBitmap(
                        MealImageThumbnailSize,
                        MealImageThumbnailSize,
                        SKColorType.Rgba8888,
                        SKAlphaType.Premul);
                    using var skCanvas = new SKCanvas(canvas);
                    skCanvas.Clear(new SKColor(27, 34, 43));
                    CollageSlot[] slots = GetCollageSlots(images.Count);

                    for (int index = 0; index < Math.Min(images.Count, slots.Length); index++) {
                        using var tile = SKBitmap.Decode(images[index]);
                        if (tile is null) {
                            continue;
                        }

                        CollageSlot slot = slots[index];
                        using SKBitmap resizedTile = CreateCroppedBitmap(tile, slot.Width, slot.Height);
                        skCanvas.DrawBitmap(resizedTile, slot.X, slot.Y, MealImageSamplingOptions);
                    }

                    return EncodeMealImage(canvas);
                } catch {
                    return null;
                }
        }
    }

    private static CollageSlot[] GetCollageSlots(int imageCount) {
        const int half = MealImageThumbnailSize / 2;
        return imageCount switch {
            2 => [
                new CollageSlot(0, 0, half, MealImageThumbnailSize),
                new CollageSlot(half, 0, half, MealImageThumbnailSize),
            ],
            3 => [
                new CollageSlot(0, 0, half, MealImageThumbnailSize),
                new CollageSlot(half, 0, half, half),
                new CollageSlot(half, half, half, half),
            ],
            _ => [
                new CollageSlot(0, 0, half, half),
                new CollageSlot(half, 0, half, half),
                new CollageSlot(0, half, half, half),
                new CollageSlot(half, half, half, half),
            ],
        };
    }

    private static SKBitmap CreateCroppedBitmap(SKBitmap source, int width, int height) {
        float scale = Math.Max((float)width / source.Width, (float)height / source.Height);
        float scaledWidth = source.Width * scale;
        float scaledHeight = source.Height * scale;
        var destination = new SKRect(
            (width - scaledWidth) / 2,
            (height - scaledHeight) / 2,
            (width + scaledWidth) / 2,
            (height + scaledHeight) / 2);
        var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);

        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        canvas.DrawBitmap(source, destination, MealImageSamplingOptions);

        return bitmap;
    }

    private static byte[] EncodeMealImage(SKBitmap image) {
        using SKData encoded = image.Encode(SKEncodedImageFormat.Jpeg, 86);
        return encoded.ToArray();
    }

    [StructLayout(LayoutKind.Auto)]
    private readonly record struct CollageSlot(int X, int Y, int Width, int Height);
}
