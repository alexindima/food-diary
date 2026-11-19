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
    [Trait("Category", "Slow")]
    public async Task WaitingForCapacityHasItsOwnDeadlineEvenWhenExistingUploadsIgnoreCancellation() {
        using var http = new HttpClient();
        RecipeVideoProcessor processor = CreateProcessor(http);
        await using var first = new SuspendedReadStream();
        await using var second = new SuspendedReadStream();
        using var harness = new CancellationTokenSource(TimeSpan.FromSeconds(100));
        Task<Result<RecipeAudio>> firstTask = processor.ExtractAudioAsync(first, sourceUrl: null, harness.Token);
        Task<Result<RecipeAudio>> secondTask = processor.ExtractAudioAsync(second, sourceUrl: null, harness.Token);
        try {
            await Task.WhenAll(first.Entered.Task, second.Entered.Task).WaitAsync(TimeSpan.FromSeconds(10), harness.Token);
            await using var queued = new MemoryStream();

            ResultAssert.Failure(await processor.ExtractAudioAsync(queued, sourceUrl: null, harness.Token), "Ai.RecipeVideoUnavailable");

            Assert.False(harness.IsCancellationRequested);
            Assert.True(queued.CanRead);
            Assert.False(firstTask.IsCompleted);
            Assert.False(secondTask.IsCompleted);
        } finally {
            first.Release();
            second.Release();
            await Task.WhenAll(firstTask, secondTask).WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
        }
        await using var retry = new MemoryStream();
        ResultAssert.Failure(await processor.ExtractAudioAsync(retry, sourceUrl: null, CancellationToken.None), "Ai.InvalidRecipeVideo");
    }

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
        await video.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellation.Token);
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => extraction.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None));
        Assert.Multiple(
            () => Assert.True(video.ReadToken.IsCancellationRequested),
            () => Assert.True(video.CanRead));
    }

    private static RecipeVideoProcessor CreateProcessor(HttpClient http) => new(http,
        Options.Create(new RecipeVideoOptions { FfmpegPath = "unused-because-copy-never-completes" }),
        NullLogger<RecipeVideoProcessor>.Instance, new RecipeYouTubeVideoSource(http));

    [ExcludeFromCodeCoverage]
    private sealed class SuspendedReadStream : MemoryStream {
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void Release() => _release.TrySetResult();
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) {
            Entered.TrySetResult();
            await _release.Task.ConfigureAwait(false);
            return 0;
        }
    }

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
