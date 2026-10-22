using System.Diagnostics;
using System.Text.Json;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Infrastructure.Providers.Options;
using FoodDiary.Modules.Ai.Infrastructure.Providers.Services.Recipes;
using FoodDiary.Results;
using FoodDiary.Testing.Assertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoodDiary.Modules.Ai.Infrastructure.Tests.Services;

[Collection("Recipe media")]
[ExcludeFromCodeCoverage]
public sealed class RecipeVideoProcessBoundaryTests(RecipeVideoProcessFixture fixture) : IClassFixture<RecipeVideoProcessFixture> {
    [WindowsMediaFact]
    public async Task PublishedState_WaitsForAWindowsReadLockAndReturnsItsExactContents() {
        string statePath = fixture.NewStatePath();
        var expected = new RecipeVideoProcessState(Environment.ProcessId, ChildPid: null, "owned-input", "owned-output", ["owned-argument"], DirectoryMode: null);
        await File.WriteAllTextAsync(statePath, JsonSerializer.Serialize(expected, JsonSerializerOptions.Web));
        using var harness = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        Task<RecipeVideoProcessState> reading;
        await using (FileStream heldFile = File.Open(statePath, FileMode.Open, FileAccess.Read, FileShare.None)) {
            reading = RecipeVideoProcessFixture.ReadStateAsync(statePath, harness.Token);
            Assert.False(reading.IsCompleted);
        }

        RecipeVideoProcessState actual = await reading.WaitAsync(TimeSpan.FromSeconds(3), harness.Token);

        Assert.Multiple(
            () => Assert.Equal(expected.Pid, actual.Pid),
            () => Assert.Null(actual.ChildPid),
            () => Assert.Equal(expected.InputPath, actual.InputPath),
            () => Assert.Equal(expected.OutputPath, actual.OutputPath),
            () => Assert.Equal(expected.Arguments, actual.Arguments),
            () => Assert.Null(actual.DirectoryMode),
            () => Assert.False(harness.IsCancellationRequested));
    }

    [WindowsMediaFact]
    public async Task PublishedState_StillHonorsCallerCancellationWhileAWindowsReadLockIsHeld() {
        string statePath = fixture.NewStatePath();
        await File.WriteAllTextAsync(statePath, "{}");
        await using FileStream heldFile = File.Open(statePath, FileMode.Open, FileAccess.Read, FileShare.None);
        using var cancellation = new CancellationTokenSource();
        Task<RecipeVideoProcessState> reading = RecipeVideoProcessFixture.ReadStateAsync(statePath, cancellation.Token);
        Assert.False(reading.IsCompleted);

        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reading.WaitAsync(TimeSpan.FromSeconds(3)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LocalProcess_PreservesSafeArgumentsAndReturnsExactlyOneSecondOfMonoAudio(bool webm) {
        const string sourceUrl = "https://example.org/fixture.mp4?token=fixture-only";
        string statePath = fixture.NewStatePath();
        await using MemoryStream video = RecipeVideoProcessFixture.Input("valid", statePath, webm);
        using var http = new HttpClient();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));

        RecipeAudio audio = ResultAssert.Success(await CreateProcessor(http).ExtractAudioAsync(video, sourceUrl, timeout.Token));

        RecipeVideoProcessState state = await RecipeVideoProcessFixture.ReadStateAsync(statePath, timeout.Token);
        AssertSafeArguments(state, webm ? "matroska" : "mov");
        Assert.Multiple(
            () => Assert.Equal(1, audio.DurationSeconds),
            () => Assert.Equal(32044, audio.Wav.Length),
            () => Assert.Equal(sourceUrl, audio.SourceUrl),
            () => Assert.True(video.CanRead),
            () => Assert.False(Directory.Exists(RecipeVideoProcessFixture.MediaDirectory(state))),
            () => Assert.DoesNotContain(sourceUrl, state.Arguments, StringComparer.Ordinal));
        if (!OperatingSystem.IsWindows()) {
            Assert.Equal((int)(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute), state.DirectoryMode);
        }
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("oversized")]
    [InlineData("zero-duration")]
    [InlineData("error")]
    public async Task InvalidOutputOrLargeDiagnostics_AreRejectedWithoutLeakingDataOrTemporaryFiles(string mode) {
        string statePath = fixture.NewStatePath();
        await using MemoryStream video = RecipeVideoProcessFixture.Input(mode, statePath);
        using var http = new HttpClient();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));

        Error error = ResultAssert.Failure(await CreateProcessor(http).ExtractAudioAsync(video, sourceUrl: null, timeout.Token), "Ai.InvalidRecipeVideo");

        RecipeVideoProcessState state = await RecipeVideoProcessFixture.ReadStateAsync(statePath, timeout.Token);
        AssertSafeArguments(state, "mov");
        Assert.True(video.CanRead);
        Assert.False(Directory.Exists(RecipeVideoProcessFixture.MediaDirectory(state)));
        Assert.DoesNotContain(new string('x', 100), error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MissingProcessExecutable_ReturnsUnavailableWithoutLeavingStagedMedia() {
        string[] before = Directory.GetDirectories(Path.GetTempPath(), "fooddiary-recipe-*");
        await using MemoryStream video = RecipeVideoProcessFixture.Input("valid", fixture.NewStatePath());
        using var http = new HttpClient();
        RecipeVideoProcessor processor = CreateProcessor(http, executable: Path.Combine(fixture.Root, "missing-media-processor"));

        ResultAssert.Failure(await processor.ExtractAudioAsync(video, sourceUrl: null, CancellationToken.None), "Ai.RecipeVideoUnavailable");

        Assert.True(video.CanRead);
        Assert.Empty(Directory.GetDirectories(Path.GetTempPath(), "fooddiary-recipe-*").Except(before, StringComparer.Ordinal));
    }

    [Fact]
    public Task CallerCancellation_KillsTheOwnedProcessTreeAndReleasesCapacity() => CancelOwnedProcessAsync(lockInput: false, CancellationToken.None);

    [WindowsMediaFact]
    public Task LockedInputCleanup_EmitsOnlyTheFixedWarningAndReleasesCapacity() => CancelOwnedProcessAsync(lockInput: true, CancellationToken.None);

    [Fact]
    [Trait("Category", "Slow")]
    public async Task NativeDeadline_ReturnsUnavailableAndKillsTheOwnedProcessTree() {
        string statePath = fixture.NewStatePath();
        await using MemoryStream video = RecipeVideoProcessFixture.Input("wait", statePath);
        using var http = new HttpClient();
        using var harness = new CancellationTokenSource(TimeSpan.FromSeconds(65));
        Task<Result<RecipeAudio>> extraction = CreateProcessor(http).ExtractAudioAsync(video, sourceUrl: null, harness.Token);
        Process? parent = null;
        Process? child = null;
        try {
            RecipeVideoProcessState state = await RecipeVideoProcessFixture.ReadStateAsync(statePath, harness.Token);
            parent = Process.GetProcessById(state.Pid);
            Assert.NotNull(state.ChildPid);
            child = Process.GetProcessById(state.ChildPid.Value);

            ResultAssert.Failure(await extraction.WaitAsync(TimeSpan.FromSeconds(55)), "Ai.RecipeVideoUnavailable");

            Assert.False(harness.IsCancellationRequested);
            await AssertExitedAsync(parent, CancellationToken.None);
            await AssertExitedAsync(child, CancellationToken.None);
            Assert.False(Directory.Exists(RecipeVideoProcessFixture.MediaDirectory(state)));
            Assert.True(video.CanRead);
            await AssertCapacityAvailableAsync(http, CancellationToken.None);
        } finally {
            await harness.CancelAsync();
            await StopOwnedProcessAsync(parent, CancellationToken.None);
            await StopOwnedProcessAsync(child, CancellationToken.None);
            try { await extraction.WaitAsync(TimeSpan.FromSeconds(10)); } catch (OperationCanceledException) { }
            parent?.Dispose();
            child?.Dispose();
        }
    }

    private async Task CancelOwnedProcessAsync(bool lockInput, CancellationToken cancellationToken) {
        string statePath = fixture.NewStatePath();
        await using MemoryStream video = RecipeVideoProcessFixture.Input("wait", statePath);
        using var http = new HttpClient();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cancellation.CancelAfter(TimeSpan.FromSeconds(20));
        var logger = new RecordingLogger();
        Task<Result<RecipeAudio>> extraction = CreateProcessor(http, logger).ExtractAudioAsync(video, sourceUrl: null, cancellation.Token);
        Process? parent = null;
        Process? child = null;
        FileStream? inputLock = null;
        RecipeVideoProcessState? state = null;
        try {
            state = await RecipeVideoProcessFixture.ReadStateAsync(statePath, cancellation.Token).ConfigureAwait(false);
            parent = Process.GetProcessById(state.Pid);
            Assert.NotNull(state.ChildPid);
            child = Process.GetProcessById(state.ChildPid.Value);
            Assert.False(parent.HasExited);
            Assert.False(child.HasExited);
            _ = RecipeVideoProcessFixture.MediaDirectory(state);
            if (lockInput) {
                inputLock = File.Open(state.InputPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            }

            await cancellation.CancelAsync().ConfigureAwait(false);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => extraction.WaitAsync(TimeSpan.FromSeconds(10))).ConfigureAwait(false);
            await AssertExitedAsync(parent, CancellationToken.None).ConfigureAwait(false);
            await AssertExitedAsync(child, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(lockInput, Directory.Exists(RecipeVideoProcessFixture.MediaDirectory(state)));
            if (lockInput) {
                Assert.Equal("Recipe media temporary directory cleanup failed.", Assert.Single(logger.Warnings));
            } else {
                Assert.Empty(logger.Warnings);
            }
            Assert.True(video.CanRead);
            await AssertCapacityAvailableAsync(http, cancellationToken).ConfigureAwait(false);
        } finally {
            await cancellation.CancelAsync().ConfigureAwait(false);
            await StopOwnedProcessAsync(parent, CancellationToken.None).ConfigureAwait(false);
            await StopOwnedProcessAsync(child, CancellationToken.None).ConfigureAwait(false);
            try { await extraction.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false); } catch (OperationCanceledException) { }
            if (inputLock is not null) { await inputLock.DisposeAsync().ConfigureAwait(false); }
            if (state is not null) {
                string directory = RecipeVideoProcessFixture.MediaDirectory(state);
                if (Directory.Exists(directory)) { Directory.Delete(directory, recursive: true); }
            }
            parent?.Dispose();
            child?.Dispose();
        }
    }

    private async Task AssertCapacityAvailableAsync(HttpClient http, CancellationToken cancellationToken) {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        for (int attempt = 0; attempt < 3; attempt++) {
            await using MemoryStream video = RecipeVideoProcessFixture.Input("valid", fixture.NewStatePath());
            RecipeAudio audio = ResultAssert.Success(await CreateProcessor(http).ExtractAudioAsync(video, sourceUrl: null, timeout.Token).ConfigureAwait(false));
            Assert.Equal(1, audio.DurationSeconds);
        }
    }

    private RecipeVideoProcessor CreateProcessor(HttpClient http, ILogger<RecipeVideoProcessor>? logger = null, string? executable = null) => new(http,
        Options.Create(new RecipeVideoOptions { FfmpegPath = executable ?? fixture.ExecutablePath }), logger ?? new RecordingLogger(), new RecipeYouTubeVideoSource(http));

    private static async Task AssertExitedAsync(Process process, CancellationToken cancellationToken) {
        await process.WaitForExitAsync(cancellationToken).WaitAsync(TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
        Assert.True(process.HasExited);
    }

    private static async Task StopOwnedProcessAsync(Process? process, CancellationToken cancellationToken) {
        if (process is null) { return; }
        if (!process.HasExited) { process.Kill(entireProcessTree: true); }
        await process.WaitForExitAsync(cancellationToken).WaitAsync(TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
    }

    private static void AssertSafeArguments(RecipeVideoProcessState state, string expectedFormat) {
        string Value(string name) {
            int index = Array.IndexOf(state.Arguments, name);
            Assert.InRange(index, 0, state.Arguments.Length - 2);
            return state.Arguments[index + 1];
        }
        Assert.Contains("-nostdin", state.Arguments, StringComparer.Ordinal);
        Assert.Multiple(
            () => Assert.Equal("file,pipe", Value("-protocol_whitelist")),
            () => Assert.Equal(expectedFormat, Value("-f")),
            () => Assert.Equal(state.InputPath, Value("-i")),
            () => Assert.Equal("1", Value("-ac")),
            () => Assert.Equal("16000", Value("-ar")),
            () => Assert.Equal("pcm_s16le", Value("-c:a")),
            () => Assert.Equal("2", Value("-threads")),
            () => Assert.Equal("1", Value("-filter_threads")),
            () => Assert.Equal("301", Value("-t")),
            () => Assert.Equal("10485760", Value("-fs")),
            () => Assert.Equal("-1", Value("-map_metadata")),
            () => Assert.Equal(state.OutputPath, state.Arguments[^1]));
    }

    [ExcludeFromCodeCoverage]
    [AttributeUsage(AttributeTargets.Method)]
    private sealed class WindowsMediaFactAttribute : FactAttribute {
        public WindowsMediaFactAttribute() {
            if (!OperatingSystem.IsWindows()) { Skip = "Windows file-sharing cleanup semantics require Windows."; }
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingLogger : ILogger<RecipeVideoProcessor> {
        public List<string> Warnings { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
            if (logLevel == LogLevel.Warning) { Warnings.Add(formatter(state, exception)); }
        }
    }
}
