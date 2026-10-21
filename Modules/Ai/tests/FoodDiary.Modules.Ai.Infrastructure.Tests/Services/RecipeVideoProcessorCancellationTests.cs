using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Infrastructure.Providers.Options;
using FoodDiary.Modules.Ai.Infrastructure.Providers.Services.Recipes;
using FoodDiary.Results;
using FoodDiary.Testing.Assertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FoodDiary.Modules.Ai.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
[Collection("Recipe media")]
public sealed class RecipeVideoProcessorCancellationTests {
    [Fact]
    public async Task UpstreamReadCancellation_ReturnsUnavailableAndReleasesProcessingCapacityWithoutClosingBorrowedStreams() {
        using var http = new HttpClient();
        RecipeVideoProcessor processor = CreateProcessor(http);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        for (int attempt = 0; attempt < 3; attempt++) {
            await using var video = new CancelledReadStream();

            ResultAssert.Failure(await processor.ExtractAudioAsync(video, sourceUrl: null, cancellation.Token), "Ai.RecipeVideoUnavailable");

            Assert.Multiple(
                () => Assert.Equal(1, video.ReadCount),
                () => Assert.True(video.CanRead),
                () => Assert.False(cancellation.IsCancellationRequested));
        }
    }

    [Fact]
    public async Task CallerCancellation_AfterCopyStarts_PropagatesAndLeavesTheBorrowedStreamOpen() {
        using var http = new HttpClient();
        RecipeVideoProcessor processor = CreateProcessor(http);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await using var video = new WaitingReadStream();

        Task<Result<RecipeAudio>> extraction = processor.ExtractAudioAsync(video, sourceUrl: null, cancellation.Token);
        await video.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => extraction.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Multiple(
            () => Assert.True(video.ReadToken.IsCancellationRequested),
            () => Assert.True(video.CanRead));
    }

    private static RecipeVideoProcessor CreateProcessor(HttpClient http) => new(http,
        Options.Create(new RecipeVideoOptions { FfmpegPath = "unused-because-copy-never-completes" }),
        NullLogger<RecipeVideoProcessor>.Instance, new RecipeYouTubeVideoSource(http));

    [ExcludeFromCodeCoverage]
    private sealed class CancelledReadStream : MemoryStream {
        public int ReadCount { get; private set; }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) {
            ReadCount++;
            return ValueTask.FromException<int>(new OperationCanceledException("The upstream read was cancelled."));
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class WaitingReadStream : MemoryStream {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public CancellationToken ReadToken { get; private set; }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) {
            ReadToken = cancellationToken;
            Entered.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
            return 0;
        }
    }
}
