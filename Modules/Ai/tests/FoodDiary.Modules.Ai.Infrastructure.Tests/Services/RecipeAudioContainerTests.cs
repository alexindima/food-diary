using System.Buffers.Binary;
using System.Text;
using FoodDiary.Modules.Ai.Infrastructure.Providers.Services.Recipes;

namespace FoodDiary.Modules.Ai.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class RecipeAudioContainerTests {
    [Theory]
    [InlineData(0)]
    [InlineData(16000)]
    [InlineData(32000)]
    [InlineData(96000)]
    public void ReadDuration_UsesPcmDataLength(int dataLength) {
        byte[] wav = CreateWav(dataLength);

        Assert.Equal(dataLength / 32000d, RecipeVideoProcessor.ReadDuration(wav));
    }

    [Theory]
    [InlineData("short")]
    [InlineData("riff")]
    [InlineData("wave")]
    [InlineData("truncated-chunk")]
    [InlineData("compressed")]
    [InlineData("stereo")]
    [InlineData("sample-rate")]
    [InlineData("sample-width")]
    [InlineData("missing-data")]
    [InlineData("short-format")]
    public void ReadDuration_RejectsMalformedOrUnsupportedContainers(string corruption) {
        byte[] wav = CreateWav(32000);
        switch (corruption) {
            case "short": wav = wav[..20]; break;
            case "riff": wav[0] = 0; break;
            case "wave": wav[8] = 0; break;
            case "truncated-chunk": BinaryPrimitives.WriteUInt32LittleEndian(wav.AsSpan(40), uint.MaxValue); break;
            case "compressed": BinaryPrimitives.WriteUInt16LittleEndian(wav.AsSpan(20), 3); break;
            case "stereo": BinaryPrimitives.WriteUInt16LittleEndian(wav.AsSpan(22), 2); break;
            case "sample-rate": BinaryPrimitives.WriteUInt32LittleEndian(wav.AsSpan(24), 48000); break;
            case "sample-width": BinaryPrimitives.WriteUInt16LittleEndian(wav.AsSpan(34), 8); break;
            case "missing-data": "JUNK"u8.CopyTo(wav.AsSpan(36)); break;
            case "short-format": BinaryPrimitives.WriteUInt32LittleEndian(wav.AsSpan(16), 0); break;
            default: throw new ArgumentOutOfRangeException(nameof(corruption));
        }

        Assert.Throws<InvalidDataException>(() => RecipeVideoProcessor.ReadDuration(wav));
    }

    [Fact]
    public void ReadDuration_SkipsUnknownChunksAndTheirOddBytePadding() {
        byte[] original = CreateWav(16000);
        byte[] wav = new byte[original.Length + 10];
        original.AsSpan(0, 12).CopyTo(wav);
        "JUNK"u8.CopyTo(wav.AsSpan(12));
        BinaryPrimitives.WriteUInt32LittleEndian(wav.AsSpan(16), 1);
        wav[20] = 42;
        original.AsSpan(12).CopyTo(wav.AsSpan(22));
        BinaryPrimitives.WriteUInt32LittleEndian(wav.AsSpan(4), (uint)(wav.Length - 8));

        Assert.Equal(0.5, RecipeVideoProcessor.ReadDuration(wav));
    }

    [Fact]
    public async Task CopyBoundedAsync_CopiesOnlyTheInputBytes() {
        byte[] bytes = Encoding.UTF8.GetBytes("video fixture");
        await using var source = new MemoryStream(bytes);
        await using var destination = new MemoryStream();

        await RecipeVideoProcessor.CopyBoundedAsync(source, destination, CancellationToken.None);

        Assert.Equal(bytes, destination.ToArray());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CopyBoundedAsync_EnforcesTheFiftyMiBUploadBoundary(bool exceedsLimit) {
        await using var source = new MemoryStream(new byte[RecipeVideoProcessor.MaximumVideoBytes + (exceedsLimit ? 1 : 0)]);
        if (exceedsLimit) {
            await Assert.ThrowsAsync<InvalidDataException>(() =>
                RecipeVideoProcessor.CopyBoundedAsync(source, Stream.Null, CancellationToken.None));
        } else {
            await RecipeVideoProcessor.CopyBoundedAsync(source, Stream.Null, CancellationToken.None);
            Assert.Equal(source.Length, source.Position);
        }
    }

    [Fact]
    public async Task CopyBoundedAsync_PropagatesCancellationBeforeWriting() {
        await using var source = new MemoryStream(new byte[16]);
        await using var destination = new MemoryStream();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            RecipeVideoProcessor.CopyBoundedAsync(source, destination, cancellation.Token));
        Assert.Equal(0, destination.Length);
    }

    private static byte[] CreateWav(int dataLength) {
        byte[] wav = new byte[44 + dataLength];
        "RIFF"u8.CopyTo(wav);
        BinaryPrimitives.WriteUInt32LittleEndian(wav.AsSpan(4), (uint)(wav.Length - 8));
        "WAVEfmt "u8.CopyTo(wav.AsSpan(8));
        BinaryPrimitives.WriteUInt32LittleEndian(wav.AsSpan(16), 16);
        BinaryPrimitives.WriteUInt16LittleEndian(wav.AsSpan(20), 1);
        BinaryPrimitives.WriteUInt16LittleEndian(wav.AsSpan(22), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(wav.AsSpan(24), 16000);
        BinaryPrimitives.WriteUInt32LittleEndian(wav.AsSpan(28), 32000);
        BinaryPrimitives.WriteUInt16LittleEndian(wav.AsSpan(32), 2);
        BinaryPrimitives.WriteUInt16LittleEndian(wav.AsSpan(34), 16);
        "data"u8.CopyTo(wav.AsSpan(36));
        BinaryPrimitives.WriteUInt32LittleEndian(wav.AsSpan(40), (uint)dataLength);
        return wav;
    }
}
