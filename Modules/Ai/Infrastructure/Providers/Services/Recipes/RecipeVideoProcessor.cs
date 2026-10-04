using System.Buffers.Binary;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using FoodDiary.Integrations.Http.Http;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Infrastructure.Providers.Options;
using FoodDiary.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoodDiary.Modules.Ai.Infrastructure.Providers.Services.Recipes;

public sealed partial class RecipeVideoProcessor(
    HttpClient httpClient, IOptions<RecipeVideoOptions> options, ILogger<RecipeVideoProcessor> logger) : IRecipeVideoProcessor {
    private const long MaximumVideoBytes = 50 * 1024 * 1024;
    private const int MaximumAudioBytes = 10 * 1024 * 1024;
    private static readonly SemaphoreSlim ProcessingSlots = new(2);

    public async Task<Result<RecipeAudio>> ExtractAudioAsync(Stream? video, string? sourceUrl, CancellationToken cancellationToken) {
        Uri? source = null;
        if (!string.IsNullOrWhiteSpace(sourceUrl) && !RecipeSourceReader.TryNormalizeUrl(sourceUrl, out source)) {
            return Result.Failure<RecipeAudio>(AiErrors.InvalidRecipeUrl());
        }
        if (video is null && source is null) {
            return Result.Failure<RecipeAudio>(AiErrors.InvalidRecipeVideo());
        }
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(75));
        await ProcessingSlots.WaitAsync(deadline.Token).ConfigureAwait(false);
        string directory = Path.Combine(Path.GetTempPath(), "fooddiary-recipe-" + Guid.NewGuid().ToString("N"));
        try {
            Directory.CreateDirectory(directory);
            if (!OperatingSystem.IsWindows()) {
                File.SetUnixFileMode(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
            string videoPath = Path.Combine(directory, "video.bin");
            FileStream destination = File.Create(videoPath);
            await using (destination.ConfigureAwait(false)) {
                if (video is not null) {
                    await CopyBoundedAsync(video, destination, deadline.Token).ConfigureAwait(false);
                } else {
                    await DownloadAsync(source!, destination, deadline.Token).ConfigureAwait(false);
                }
            }
            byte[] header = new byte[12];
            FileStream input = File.OpenRead(videoPath);
            await using (input.ConfigureAwait(false)) {
                if (input.Length < header.Length) {
                    throw new InvalidDataException("Incomplete video container.");
                }
                await input.ReadExactlyAsync(header, deadline.Token).ConfigureAwait(false);
            }
            string format;
            if (header.AsSpan(4, 4).SequenceEqual("ftyp"u8)) {
                format = "mov";
            } else if (header.AsSpan(0, 4).SequenceEqual(new byte[] { 0x1a, 0x45, 0xdf, 0xa3 })) {
                format = "matroska";
            } else {
                throw new InvalidDataException("Unsupported video container.");
            }
            string audioPath = Path.Combine(directory, "audio.wav");
            await ExtractLocalAsync(videoPath, audioPath, format, deadline.Token).ConfigureAwait(false);
            var audioFile = new FileInfo(audioPath);
            if (!audioFile.Exists || audioFile.Length > MaximumAudioBytes) {
                throw new InvalidDataException("Invalid audio size.");
            }
            byte[] audio = await File.ReadAllBytesAsync(audioPath, deadline.Token).ConfigureAwait(false);
            double duration = ReadDuration(audio);
            if (duration is <= 0 or > 300) {
                throw new InvalidDataException("Audio duration exceeds the supported limit.");
            }
            return Result.Success(new RecipeAudio(audio, duration, source?.AbsoluteUri));
        } catch (InvalidDataException) {
            return Result.Failure<RecipeAudio>(AiErrors.InvalidRecipeVideo());
        } catch (Exception ex) when (ex is HttpRequestException or IOException or Win32Exception or UriFormatException or TimeoutException) {
            return Result.Failure<RecipeAudio>(AiErrors.RecipeVideoUnavailable());
        } catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
            return Result.Failure<RecipeAudio>(AiErrors.RecipeVideoUnavailable());
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

    private async Task DownloadAsync(Uri url, Stream destination, CancellationToken cancellationToken) {
        bool inspectedPage = false;
        for (int hop = 0; hop < 5; hop++) {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("FoodDiary-RecipeImport/1.0");
            using HttpResponseMessage response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            if ((int)response.StatusCode is >= 300 and <= 399) {
                url = NormalizeTarget(url, response.Headers.Location?.ToString());
                continue;
            }
            if (!response.IsSuccessStatusCode) {
                throw new HttpRequestException("Video source is unavailable.");
            }
            string? mediaType = response.Content.Headers.ContentType?.MediaType;
            if (!inspectedPage && string.Equals(mediaType, "text/html", StringComparison.Ordinal)) {
                inspectedPage = true;
                string html = await BoundedHttpContentReader.ReadAsStringAsync(response.Content, 1024 * 1024, TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
                url = NormalizeTarget(url, FindVideoUrl(html));
                continue;
            }
            if (mediaType is not ("video/mp4" or "video/webm" or "application/octet-stream") ||
                response.Content.Headers.ContentLength > MaximumVideoBytes) {
                throw new HttpRequestException("Unsupported video source.");
            }
            Stream body = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using (body.ConfigureAwait(false)) {
                await CopyBoundedAsync(body, destination, cancellationToken).ConfigureAwait(false);
                return;
            }
        }
        throw new HttpRequestException("Video source redirect limit exceeded.");
    }

    private static Uri NormalizeTarget(Uri basis, string? target) {
        if (string.IsNullOrWhiteSpace(target) || !RecipeSourceReader.TryNormalizeUrl(new Uri(basis, target).AbsoluteUri, out Uri? normalized)) {
            throw new HttpRequestException("No public video URL found.");
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

    private static async Task CopyBoundedAsync(Stream source, Stream destination, CancellationToken cancellationToken) {
        byte[] buffer = new byte[64 * 1024];
        long total = 0;
        int read;
        while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0) {
            total += read;
            if (total > MaximumVideoBytes) {
                throw new InvalidDataException("Video exceeds the supported limit.");
            }
            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ExtractLocalAsync(string input, string output, string format, CancellationToken cancellationToken) {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(40));
        var start = new ProcessStartInfo(options.Value.FfmpegPath) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true };
        string[] arguments = ["-nostdin", "-hide_banner", "-v", "error", "-protocol_whitelist", "file,pipe", "-f", format, "-i", input,
            "-map", "0:a:0", "-vn", "-sn", "-dn", "-map_metadata", "-1", "-map_chapters", "-1", "-ac", "1", "-ar", "16000",
            "-c:a", "pcm_s16le", "-threads", "2", "-filter_threads", "1", "-t", "301", "-fs", MaximumAudioBytes.ToString(System.Globalization.CultureInfo.InvariantCulture), output];
        foreach (string argument in arguments) {
            start.ArgumentList.Add(argument);
        }
        using Process process = Process.Start(start) ?? throw new IOException("Audio processor did not start.");
        Task drain = process.StandardError.BaseStream.CopyToAsync(Stream.Null);
        try {
            await process.WaitForExitAsync(deadline.Token).ConfigureAwait(false);
            await drain.ConfigureAwait(false);
            if (process.ExitCode != 0) {
                throw new InvalidDataException("No supported audio track found.");
            }
        } finally {
            if (!process.HasExited) {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            }
            await drain.ConfigureAwait(false);
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
