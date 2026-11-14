using System.Globalization;
using System.Buffers.Binary;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using FoodDiary.Integrations.Http.Http;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Infrastructure.Diagnostics;
using FoodDiary.Modules.Ai.Infrastructure.Providers.Options;
using FoodDiary.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoodDiary.Modules.Ai.Infrastructure.Providers.Services.Recipes;

public sealed partial class RecipeVideoProcessor(
    HttpClient httpClient, IOptions<RecipeVideoOptions> options, ILogger<RecipeVideoProcessor> logger,
    RecipeYouTubeVideoSource youTube) : IRecipeVideoProcessor {
    internal const long MaximumVideoBytes = 50 * 1024 * 1024;
    private const int MaximumAudioBytes = 10 * 1024 * 1024;
    private static readonly SemaphoreSlim ProcessingSlots = new(2);

    public async Task<Result<RecipeAudio>> ExtractAudioAsync(Stream? video, string? sourceUrl, CancellationToken cancellationToken) {
        Uri? source = null;
        if (!string.IsNullOrWhiteSpace(sourceUrl) && !RecipeSourceReader.TryNormalizeUrl(sourceUrl, out source)) {
            return Fail(AiErrors.InvalidRecipeUrl(), "validate_input", "invalid_source_url");
        }
        if (video is null && source is null) {
            return Fail(AiErrors.InvalidRecipeVideo(), "validate_input", "missing_video");
        }
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(75));
        try {
            await ProcessingSlots.WaitAsync(deadline.Token);
        } catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
            return Fail(AiErrors.RecipeVideoUnavailable(), "wait_for_processor", "deadline_expired");
        }
        string directory = Path.Combine(Path.GetTempPath(), "fooddiary-recipe-" + Guid.NewGuid().ToString("N"));
        string stage = "prepare_media";
        try {
            Directory.CreateDirectory(directory);
            RestrictUnixDirectoryPermissions(directory);
            string videoPath = Path.Combine(directory, "video.bin");
            FileStream destination = File.Create(videoPath);
            await using (destination) {
                if (video is not null) {
                    stage = "read_upload";
                    await CopyBoundedAsync(video, destination, deadline.Token);
                } else {
                    stage = "download_source";
                    await DownloadAsync(source!, destination, deadline.Token);
                }
            }
            stage = "validate_container";
            byte[] header = new byte[12];
            FileStream input = File.OpenRead(videoPath);
            await using (input) {
                if (input.Length < header.Length) {
                    return Fail(AiErrors.InvalidRecipeVideo(), stage, "incomplete_container");
                }
                await input.ReadExactlyAsync(header, deadline.Token);
            }
            string format;
            if (header.AsSpan(4, 4).SequenceEqual("ftyp"u8)) {
                format = "mov";
            } else if (header.AsSpan(0, 4).SequenceEqual(new byte[] { 0x1a, 0x45, 0xdf, 0xa3 })) {
                format = "matroska";
            } else {
                return Fail(AiErrors.InvalidRecipeVideo(), stage, "unsupported_container");
            }
            string audioPath = Path.Combine(directory, "audio.wav");
            stage = "extract_audio";
            await ExtractLocalAsync(videoPath, audioPath, format, deadline.Token);
            var audioFile = new FileInfo(audioPath);
            if (!audioFile.Exists || audioFile.Length > MaximumAudioBytes) {
                return Fail(AiErrors.InvalidRecipeVideo(), stage, "invalid_audio_size");
            }
            byte[] audio = await File.ReadAllBytesAsync(audioPath, deadline.Token);
            stage = "validate_audio";
            double duration = ReadDuration(audio);
            if (duration is <= 0 or > 300) {
                return Fail(AiErrors.InvalidRecipeVideo(), stage, "invalid_audio_duration");
            }
            return Result.Success(new RecipeAudio(audio, duration, source?.AbsoluteUri));
        } catch (RecipeVideoSourceException failure) {
            Error error = failure.InvalidSource ? AiErrors.InvalidRecipeVideoSource() : AiErrors.RecipeVideoUnavailable();
            return Fail(error, stage, failure.Reason);
        } catch (InvalidDataException) {
            string reason = stage switch {
                "download_source" => "invalid_source_media",
                "read_upload" => "media_size_limit",
                "extract_audio" => "no_supported_audio_track",
                _ => "invalid_audio_container",
            };
            return Fail(AiErrors.InvalidRecipeVideo(), stage, reason);
        } catch (HttpRequestException failure) when (failure.GetBaseException() is RecipeSourceReader.NonPublicRecipeSourceException) {
            return Fail(AiErrors.InvalidRecipeVideoSource(), stage, "non_public_destination");
        } catch (HttpRequestException failure) when (failure.GetBaseException() is SocketException {
            SocketErrorCode: SocketError.HostNotFound or SocketError.NoData,
        }) {
            return Fail(AiErrors.InvalidRecipeVideoSource(), stage, "source_host_not_found");
        } catch (HttpRequestException failure) {
            string reason = failure.StatusCode is { } statusCode
                ? FormattableString.Invariant($"upstream_http_{(int)statusCode}")
                : "upstream_request_failed";
            return Fail(AiErrors.RecipeVideoUnavailable(), stage, reason);
        } catch (Win32Exception) {
            return Fail(AiErrors.RecipeVideoUnavailable(), stage, "audio_processor_start_failed");
        } catch (Exception failure) when (failure is IOException or UnauthorizedAccessException) {
            return Fail(AiErrors.RecipeVideoUnavailable(), stage, "media_io_failed");
        } catch (TimeoutException) {
            return Fail(AiErrors.RecipeVideoUnavailable(), stage, "deadline_expired");
        } catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
            return Fail(AiErrors.RecipeVideoUnavailable(), stage, "deadline_expired");
        } finally {
            try {
                // This directory is generated locally; no user-controlled paths enter cleanup.
                if (Directory.Exists(directory)) {
                    Directory.Delete(directory, recursive: true);
                }
            } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
                logger.LogWarning("Recipe media temporary directory cleanup failed.");
            }
            ProcessingSlots.Release();
        }
    }

    private Result<RecipeAudio> Fail(Error error, string stage, string reason) {
        LogLevel level = error.Kind == ErrorKind.Validation ? LogLevel.Information : LogLevel.Warning;
        logger.Log(level, "Recipe video import failed at {Stage}: {Reason}", stage, reason);
        return Result.Failure<RecipeAudio>(error);
    }

    [ExcludeFromWindowsCoverage]
    private static void RestrictUnixDirectoryPermissions(string directory) {
        if (!OperatingSystem.IsWindows()) {
            File.SetUnixFileMode(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    private async Task DownloadAsync(Uri url, Stream destination, CancellationToken cancellationToken) {
        bool inspectedPage = false;
        for (int hop = 0; hop < 5; hop++) {
            if (RecipeYouTubeVideoSource.IsYouTubeUrl(url)) {
                await youTube.DownloadAsync(url, destination, cancellationToken);
                return;
            }
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("FoodDiary-RecipeImport/1.0");
            using HttpResponseMessage response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if ((int)response.StatusCode is >= 300 and <= 399) {
                url = NormalizeTarget(url, response.Headers.Location?.ToString(), "invalid_redirect");
                continue;
            }
            if (!response.IsSuccessStatusCode) {
                int status = (int)response.StatusCode;
                if (status is >= 400 and < 500 and not (408 or 429)) {
                    throw new RecipeVideoSourceException(FormattableString.Invariant($"source_http_{status}"));
                }
                throw new HttpRequestException("Video source request failed.", inner: null, response.StatusCode);
            }
            string? mediaType = response.Content.Headers.ContentType?.MediaType;
            if (!inspectedPage && string.Equals(mediaType, "text/html", StringComparison.Ordinal)) {
                inspectedPage = true;
                string html = await BoundedHttpContentReader.ReadAsStringAsync(response.Content, 1024 * 1024, TimeSpan.FromSeconds(10), cancellationToken);
                url = NormalizeTarget(url, FindVideoUrl(html), "no_public_video_url");
                continue;
            }
            if (mediaType is not ("video/mp4" or "video/webm" or "application/octet-stream")) {
                throw new RecipeVideoSourceException("unsupported_media_type");
            }
            if (response.Content.Headers.ContentLength > MaximumVideoBytes) {
                throw new RecipeVideoSourceException("media_size_limit");
            }
            Stream body = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using (body) {
                await CopyBoundedAsync(body, destination, cancellationToken);
            }
            return;
        }
        throw new RecipeVideoSourceException("redirect_limit");
    }

    private static Uri NormalizeTarget(Uri basis, string? target, string reason) {
        if (string.IsNullOrWhiteSpace(target) || !Uri.TryCreate(basis, target, out Uri? resolved) ||
            !RecipeSourceReader.TryNormalizeUrl(resolved.AbsoluteUri, out Uri? normalized)) {
            throw new RecipeVideoSourceException(reason);
        }
        return normalized!;
    }

    public static string? FindVideoUrl(string html) {
        foreach (Match tag in VideoTags().Matches(html)) {
            var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match attribute in HtmlAttributes().Matches(tag.Value)) {
                attributes[attribute.Groups["name"].Value] = WebUtility.HtmlDecode(attribute.Groups["value"].Value);
            }
            if (attributes.TryGetValue("property", out string? property) &&
                property is "og:video" or "og:video:url" or "og:video:secure_url" && attributes.TryGetValue("content", out string? content)) {
                return content;
            }
            if (!tag.Value.StartsWith("<meta", StringComparison.OrdinalIgnoreCase) && attributes.TryGetValue("src", out string? src)) {
                return src;
            }
        }
        return null;
    }

    internal static async Task CopyBoundedAsync(Stream source, Stream destination, CancellationToken cancellationToken) {
        byte[] buffer = new byte[64 * 1024];
        long total = 0;
        int read;
        while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0) {
            total += read;
            if (total > MaximumVideoBytes) {
                throw new InvalidDataException("Video exceeds the supported limit.");
            }
            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
    }

    private async Task ExtractLocalAsync(string input, string output, string format, CancellationToken cancellationToken) {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(40));
        var start = new ProcessStartInfo(options.Value.FfmpegPath) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true };
        string[] arguments = ["-nostdin", "-hide_banner", "-v", "error", "-protocol_whitelist", "file,pipe", "-f", format, "-i", input,
            "-map", "0:a:0", "-vn", "-sn", "-dn", "-map_metadata", "-1", "-map_chapters", "-1", "-ac", "1", "-ar", "16000",
            "-c:a", "pcm_s16le", "-threads", "2", "-filter_threads", "1", "-t", "301", "-fs", MaximumAudioBytes.ToString(CultureInfo.InvariantCulture), output];
        foreach (string argument in arguments) {
            start.ArgumentList.Add(argument);
        }
        using Process process = Process.Start(start) ?? throw new IOException("Audio processor did not start.");
        // Keep draining stderr until the process exits, including during cancellation cleanup.
        Task drain = process.StandardError.BaseStream.CopyToAsync(Stream.Null, CancellationToken.None);
        try {
            await process.WaitForExitAsync(deadline.Token);
            await drain;
            if (process.ExitCode != 0) {
                throw new InvalidDataException("No supported audio track found.");
            }
        } finally {
            if (!process.HasExited) {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(CancellationToken.None);
            }
            await drain;
        }
    }

    public static double ReadDuration(byte[] wav) {
        if (wav.Length < 44 || !wav.AsSpan(0, 4).SequenceEqual("RIFF"u8) || !wav.AsSpan(8, 4).SequenceEqual("WAVE"u8)) {
            throw new InvalidDataException("Invalid audio container.");
        }
        bool validFormat = false;
        for (int offset = 12; offset <= wav.Length - 8;) {
            uint length = BinaryPrimitives.ReadUInt32LittleEndian(wav.AsSpan(offset + 4, 4));
            if (length > wav.Length - offset - 8) {
                throw new InvalidDataException("Invalid audio chunk.");
            }
            if (wav.AsSpan(offset, 4).SequenceEqual("fmt "u8) && length >= 16) {
                validFormat = BinaryPrimitives.ReadUInt16LittleEndian(wav.AsSpan(offset + 8, 2)) == 1 &&
                    BinaryPrimitives.ReadUInt16LittleEndian(wav.AsSpan(offset + 10, 2)) == 1 &&
                    BinaryPrimitives.ReadUInt32LittleEndian(wav.AsSpan(offset + 12, 4)) == 16000 &&
                    BinaryPrimitives.ReadUInt16LittleEndian(wav.AsSpan(offset + 22, 2)) == 16;
            }
            if (wav.AsSpan(offset, 4).SequenceEqual("data"u8) && validFormat) {
                return length / 32000d;
            }
            offset = checked(offset + 8 + (int)length + ((int)length & 1));
        }
        throw new InvalidDataException("No supported audio data found.");
    }

    [GeneratedRegex("<(?:meta|video|source)\\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking)]
    private static partial Regex VideoTags();
    [GeneratedRegex("(?<name>[\\w:-]+)\\s*=\\s*[\"'](?<value>[^\"']*)[\"']", RegexOptions.NonBacktracking)]
    private static partial Regex HtmlAttributes();
}
