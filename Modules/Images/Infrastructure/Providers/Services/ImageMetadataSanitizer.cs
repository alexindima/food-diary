using System.Buffers.Binary;
using SkiaSharp;

namespace FoodDiary.Modules.Images.Infrastructure.Providers.Services;

internal static class ImageMetadataSanitizer {
    public static byte[]? Sanitize(ReadOnlySpan<byte> content, SKCodec codec, SKBitmap pixels) {
        // Bake orientation into the pixels when removing its metadata would turn the image.
        if (codec.EncodedFormat == SKEncodedImageFormat.Jpeg || codec.EncodedOrigin != SKEncodedOrigin.TopLeft) {
            return codec.FrameCount <= 1 ? EncodeOriented(codec, pixels) : null;
        }

        return codec.EncodedFormat switch {
            SKEncodedImageFormat.Png when content.Length >= 8 => StripPng(content),
            SKEncodedImageFormat.Webp when content.Length >= 12 => StripWebp(content),
            SKEncodedImageFormat.Gif when content.Length >= 13 => StripGif(content),
            _ => null,
        };
    }

    private static byte[]? EncodeOriented(SKCodec codec, SKBitmap pixels) {
        int width = pixels.Width;
        int height = pixels.Height;
        SKEncodedOrigin origin = codec.EncodedOrigin;
        SKMatrix orientation = OrientationMatrix(origin, width, height);
        if (orientation.IsIdentity) {
            using SKData? encoded = pixels.Encode(codec.EncodedFormat, quality: 90);
            return encoded?.ToArray();
        }

        bool swapDimensions = origin is >= SKEncodedOrigin.LeftTop and <= SKEncodedOrigin.LeftBottom;
        var orientedInfo = new SKImageInfo(swapDimensions ? height : width, swapDimensions ? width : height,
            pixels.ColorType, pixels.AlphaType);
        using var oriented = new SKBitmap(orientedInfo);
        using (var canvas = new SKCanvas(oriented)) {
            canvas.Clear(SKColors.Transparent);
            canvas.SetMatrix(orientation);
            canvas.DrawBitmap(pixels, 0, 0, new SKSamplingOptions(SKFilterMode.Nearest));
        }
        using SKData? result = oriented.Encode(codec.EncodedFormat, quality: 90);
        return result?.ToArray();
    }

    private static SKMatrix OrientationMatrix(SKEncodedOrigin origin, int width, int height) => origin switch {
        SKEncodedOrigin.TopRight => new(-1, 0, width, 0, 1, 0, 0, 0, 1),
        SKEncodedOrigin.BottomRight => new(-1, 0, width, 0, -1, height, 0, 0, 1),
        SKEncodedOrigin.BottomLeft => new(1, 0, 0, 0, -1, height, 0, 0, 1),
        SKEncodedOrigin.LeftTop => new(0, 1, 0, 1, 0, 0, 0, 0, 1),
        SKEncodedOrigin.RightTop => new(0, -1, height, 1, 0, 0, 0, 0, 1),
        SKEncodedOrigin.RightBottom => new(0, -1, height, -1, 0, width, 0, 0, 1),
        SKEncodedOrigin.LeftBottom => new(0, 1, 0, -1, 0, width, 0, 0, 1),
        _ => SKMatrix.Identity,
    };

    private static byte[]? StripPng(ReadOnlySpan<byte> content) {
        const int signatureLength = 8;
        using var output = new MemoryStream(content.Length);
        output.Write(content[..signatureLength]);
        content = content[signatureLength..];
        while (content.Length >= 12) {
            uint payloadLength = BinaryPrimitives.ReadUInt32BigEndian(content);
            if (payloadLength > content.Length - 12) {
                return null;
            }

            int chunkLength = (int)payloadLength + 12;
            ReadOnlySpan<byte> kind = content.Slice(4, 4);
            if (kind.SequenceEqual("IEND"u8)) {
                output.Write(content[..chunkLength]);
                return payloadLength == 0 ? output.ToArray() : null;
            }

            // Keep pixel, palette, display color and animation chunks. Drop all free-form ancillary data.
            if (kind.SequenceEqual("IHDR"u8) || kind.SequenceEqual("PLTE"u8) || kind.SequenceEqual("IDAT"u8) ||
                kind.SequenceEqual("tRNS"u8) || kind.SequenceEqual("sRGB"u8) || kind.SequenceEqual("gAMA"u8) ||
                kind.SequenceEqual("cHRM"u8) || kind.SequenceEqual("cICP"u8) || kind.SequenceEqual("sBIT"u8) ||
                kind.SequenceEqual("bKGD"u8) || kind.SequenceEqual("pHYs"u8) ||
                kind.SequenceEqual("acTL"u8) || kind.SequenceEqual("fcTL"u8) || kind.SequenceEqual("fdAT"u8)) {
                output.Write(content[..chunkLength]);
            }
            content = content[chunkLength..];
        }
        return null;
    }

    private static byte[]? StripWebp(ReadOnlySpan<byte> content) {
        const int headerLength = 12;
        uint riffLength = BinaryPrimitives.ReadUInt32LittleEndian(content[4..]);
        if (riffLength < 4 || riffLength > content.Length - 8) {
            return null;
        }

        using var output = new MemoryStream(content.Length);
        output.Write(content[..headerLength]);
        if (!WriteWebpChunks(content.Slice(headerLength, (int)riffLength - 4), output, frame: false)) {
            return null;
        }

        byte[] result = output.ToArray();
        BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(4, 4), (uint)(result.Length - 8));
        return result;
    }

    private static bool WriteWebpChunks(ReadOnlySpan<byte> content, Stream output, bool frame) {
        Span<byte> extendedHeader = stackalloc byte[10];
        while (content.Length >= 8) {
            ReadOnlySpan<byte> kind = content[..4];
            uint length = BinaryPrimitives.ReadUInt32LittleEndian(content[4..]);
            if (length > content.Length - 8 || length + (length & 1) > content.Length - 8) {
                return false;
            }

            ReadOnlySpan<byte> payload = content.Slice(8, (int)length);
            if (kind.SequenceEqual("VP8 "u8) || kind.SequenceEqual("VP8L"u8) || kind.SequenceEqual("ALPH"u8)) {
                WriteWebpChunk(output, kind, payload);
            } else if (!frame && kind.SequenceEqual("VP8X"u8) && length == 10) {
                payload.CopyTo(extendedHeader);
                // Only alpha and animation remain. ICC, EXIF and XMP chunks are discarded.
                extendedHeader[0] &= 0x12;
                extendedHeader[1..4].Clear();
                WriteWebpChunk(output, kind, extendedHeader);
            } else if (!frame && kind.SequenceEqual("ANIM"u8) && length == 6) {
                WriteWebpChunk(output, kind, payload);
            } else if (!frame && kind.SequenceEqual("ANMF"u8) && length >= 16) {
                using var frameData = new MemoryStream((int)length);
                frameData.Write(payload[..16]);
                if (!WriteWebpChunks(payload[16..], frameData, frame: true)) {
                    return false;
                }
                WriteWebpChunk(output, kind, frameData.GetBuffer().AsSpan(0, (int)frameData.Length));
            }
            content = content[(8 + (int)length + (int)(length & 1))..];
        }
        return content.IsEmpty;
    }

    private static void WriteWebpChunk(Stream output, ReadOnlySpan<byte> kind, ReadOnlySpan<byte> payload) {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(length, (uint)payload.Length);
        output.Write(kind);
        output.Write(length);
        output.Write(payload);
        if ((payload.Length & 1) != 0) {
            output.WriteByte(0);
        }
    }

    private static byte[]? StripGif(ReadOnlySpan<byte> content) {
        int headerLength = 13 + GifColorTableLength(content[10]);
        if (headerLength > content.Length) {
            return null;
        }

        using var output = new MemoryStream(content.Length);
        output.Write(content[..headerLength]);
        content = content[headerLength..];
        while (!content.IsEmpty) {
            switch (content[0]) {
                case 0x3b: // Trailer
                    output.WriteByte(0x3b);
                    return output.ToArray();
                case 0x2c when content.Length >= 10: { // Image descriptor
                        int blocks = 11 + GifColorTableLength(content[9]); // Includes the LZW code size.
                        if (blocks >= content.Length || !TryMeasureGifBlocks(content[blocks..], out int length)) {
                            return null;
                        }
                        int imageLength = blocks + length;
                        output.Write(content[..imageLength]);
                        content = content[imageLength..];
                        break;
                    }
                case 0x21 when content.Length >= 3: { // Extension
                        if (!TryMeasureGifBlocks(content[2..], out int length)) {
                            return null;
                        }
                        ReadOnlySpan<byte> extension = content[..(length + 2)];
                        if (IsGifAnimationExtension(extension)) {
                            output.Write(extension);
                        }
                        content = content[extension.Length..];
                        break;
                    }
                default:
                    return null;
            }
        }
        return null;
    }

    private static bool IsGifAnimationExtension(ReadOnlySpan<byte> extension) =>
        (extension[1] == 0xf9 && extension.Length == 8 && extension[2] == 4) ||
        (extension[1] == 0xff && extension.Length == 19 && extension[2] == 11 &&
         extension.Slice(3, 11).SequenceEqual("NETSCAPE2.0"u8) && extension[14] == 3 && extension[15] == 1);

    private static int GifColorTableLength(byte flags) => (flags & 0x80) == 0 ? 0 : 3 << ((flags & 7) + 1);

    private static bool TryMeasureGifBlocks(ReadOnlySpan<byte> content, out int length) {
        length = 0;
        while (length < content.Length) {
            int blockLength = content[length++];
            if (blockLength == 0) {
                return true;
            }
            if (blockLength > content.Length - length) {
                return false;
            }
            length += blockLength;
        }
        return false;
    }
}
