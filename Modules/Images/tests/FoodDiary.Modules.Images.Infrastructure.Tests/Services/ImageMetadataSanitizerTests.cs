using System.Buffers.Binary;
using FoodDiary.Modules.Images.Infrastructure.Providers.Services;
using SkiaSharp;

namespace FoodDiary.Modules.Images.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class ImageMetadataSanitizerTests {
    [Fact]
    public void UnsupportedBitmapFormatProducesNoPublishableContent() {
        using var bitmap = new SKBitmap(2, 2);
        byte[] source = Convert.FromHexString("424D3A0000000000000036000000280000000100000001000000010018000000000004000000000000000000000000000000000000000000FF00");
        using var stream = new SKMemoryStream(source);
        using var codec = SKCodec.Create(stream);
        Assert.NotNull(codec);

        Assert.Null(ImageMetadataSanitizer.Sanitize(source, codec, bitmap));
    }

    [Theory]
    [InlineData(1, 0, false)]
    [InlineData(2, 1, false)]
    [InlineData(3, 3, false)]
    [InlineData(4, 2, false)]
    [InlineData(5, 0, true)]
    [InlineData(6, 2, true)]
    [InlineData(7, 3, true)]
    [InlineData(8, 1, true)]
    public void JpegOrientationIsBakedIntoThePixelsBeforeExifIsRemoved(byte orientation, int firstCorner, bool swapsDimensions) {
        SKColor[] colors = [SKColors.Red, SKColors.Lime, SKColors.Blue, SKColors.Yellow];
        using var original = new SKBitmap(48, 32);
        using (var canvas = new SKCanvas(original)) {
            for (int corner = 0; corner < colors.Length; corner++) {
                using var paint = new SKPaint { Color = colors[corner] };
                canvas.DrawRect(new SKRect((corner % 2) * 24, (corner / 2) * 16,
                    ((corner % 2) + 1) * 24, ((corner / 2) + 1) * 16), paint);
            }
        }
        using SKData jpeg = original.Encode(SKEncodedImageFormat.Jpeg, 100);
        byte[] exif = Convert.FromHexString("FFE1002245786966000049492A0008000000010012010300010000000600000000000000");
        exif[28] = orientation;
        byte[] source = [.. jpeg.Span[..2], .. exif, .. jpeg.Span[2..]];
        using var stream = new SKMemoryStream(source);
        using var codec = SKCodec.Create(stream);
        Assert.NotNull(codec);
        Assert.Equal((SKEncodedOrigin)orientation, codec.EncodedOrigin);
        using var pixels = SKBitmap.Decode(source);

        byte[] published = Assert.IsType<byte[]>(ImageMetadataSanitizer.Sanitize(source, codec, pixels));

        using var result = SKBitmap.Decode(published);
        Assert.Multiple(
            () => Assert.Equal(swapsDimensions ? 32 : 48, result.Width),
            () => Assert.Equal(swapsDimensions ? 48 : 32, result.Height),
            () => Assert.Equal(-1, published.AsSpan().IndexOf("Exif\0\0"u8)));
        SKColor actual = result.GetPixel(result.Width / 4, result.Height / 4);
        SKColor expected = colors[firstCorner];
        Assert.Multiple(
            () => Assert.InRange(Math.Abs(actual.Red - expected.Red), 0, 20),
            () => Assert.InRange(Math.Abs(actual.Green - expected.Green), 0, 20),
            () => Assert.InRange(Math.Abs(actual.Blue - expected.Blue), 0, 20));
    }

    [Theory]
    [InlineData("png", "oversized-chunk")]
    [InlineData("png", "missing-end")]
    [InlineData("png", "end-has-payload")]
    [InlineData("webp", "short-riff")]
    [InlineData("webp", "oversized-riff")]
    [InlineData("webp", "oversized-chunk")]
    [InlineData("webp", "missing-padding")]
    [InlineData("webp", "trailing-byte")]
    [InlineData("webp", "malformed-animation-frame")]
    [InlineData("gif", "missing-color-table")]
    [InlineData("gif", "missing-trailer")]
    [InlineData("gif", "unknown-block")]
    [InlineData("gif", "short-image")]
    [InlineData("gif", "oversized-image-block")]
    [InlineData("gif", "oversized-extension-block")]
    [InlineData("gif", "unterminated-extension")]
    public void MalformedContainersProduceNoSanitizedOutput(string format, string corruption) {
        using var bitmap = new SKBitmap(2, 2);
        bitmap.Erase(SKColors.Red);
        SKEncodedImageFormat encodedFormat = format switch {
            "png" => SKEncodedImageFormat.Png,
            "webp" => SKEncodedImageFormat.Webp,
            _ => SKEncodedImageFormat.Gif,
        };
        byte[] original = format is "gif"
            ? Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAP///ywAAAAAAQABAAACAUwAOw==")
            : Encode(bitmap, encodedFormat);
        using var stream = new SKMemoryStream(original);
        using var codec = SKCodec.Create(stream);
        Assert.NotNull(codec);
        byte[] broken = Corrupt(original, format, corruption);

        Assert.Null(ImageMetadataSanitizer.Sanitize(broken, codec, bitmap));
    }

    private static byte[] Encode(SKBitmap bitmap, SKEncodedImageFormat format) {
        using SKData encoded = bitmap.Encode(format, 100);
        return encoded.ToArray();
    }

    private static byte[] Corrupt(byte[] original, string format, string corruption) {
        if (format is "png") {
            if (corruption is "missing-end") {
                return original[..^12];
            }
            if (corruption is "end-has-payload") {
                return [.. original[..^12], 0, 0, 0, 1, (byte)'I', (byte)'E', (byte)'N', (byte)'D', 0, 0, 0, 0, 0];
            }
            BinaryPrimitives.WriteUInt32BigEndian(original.AsSpan(8), uint.MaxValue);
            return original;
        }
        if (format is "webp") {
            switch (corruption) {
                case "short-riff": BinaryPrimitives.WriteUInt32LittleEndian(original.AsSpan(4), 3); return original;
                case "oversized-riff": BinaryPrimitives.WriteUInt32LittleEndian(original.AsSpan(4), uint.MaxValue); return original;
                case "oversized-chunk": BinaryPrimitives.WriteUInt32LittleEndian(original.AsSpan(16), uint.MaxValue); return original;
            }
            byte[] chunks = corruption switch {
                "missing-padding" => [(byte)'X', (byte)'M', (byte)'P', (byte)' ', 1, 0, 0, 0, 1],
                "malformed-animation-frame" => [.. "ANMF"u8, 17, 0, 0, 0, .. new byte[16], 255, 0],
                _ => [255],
            };
            byte[] result = [.. original[..12], .. chunks];
            BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(4), (uint)(result.Length - 8));
            return result;
        }
        byte[] header = original[..19];
        return corruption switch {
            "missing-color-table" => [.. original[..13]],
            "missing-trailer" => header,
            "unknown-block" => [.. header, 255],
            "short-image" => [.. header, 0x2c, .. new byte[9]],
            "oversized-image-block" => [.. header, 0x2c, .. new byte[9], 2, 5, 1],
            "oversized-extension-block" => [.. header, 0x21, 0xfe, 5, 1],
            _ => [.. header, 0x21, 0xfe, 1, 1],
        };
    }
}
