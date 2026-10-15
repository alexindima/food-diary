using YoutubeExplode;
using YoutubeExplode.Exceptions;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace FoodDiary.Modules.Ai.Infrastructure.Providers.Services.Recipes;

public sealed class RecipeYouTubeVideoSource(HttpClient httpClient) {
    internal static bool IsYouTubeUrl(Uri url) => url.IdnHost is
        "youtube.com" or "www.youtube.com" or "m.youtube.com" or "youtu.be";

    public async Task DownloadAsync(Uri source, Stream destination, CancellationToken cancellationToken) {
        VideoId? id = IsYouTubeUrl(source) ? VideoId.TryParse(source.AbsoluteUri) : null;
        if (id is null) {
            throw new HttpRequestException("A YouTube video URL is required.");
        }
        try {
            // The injected client retains public-DNS validation, HTTPS/host gates and response bounds.
            // Only the audio stream is downloaded; FFmpeg subsequently reads a local file only.
            using var youtube = new YoutubeClient(httpClient);
            Video video = await youtube.Videos.GetAsync(id.Value, cancellationToken).ConfigureAwait(false);
            if (video.Duration is null || video.Duration.Value.TotalSeconds is <= 0 or > 300) {
                throw new InvalidDataException("Video must have a known duration of at most five minutes.");
            }
            StreamManifest manifest = await youtube.Videos.Streams.GetManifestAsync(id.Value, cancellationToken).ConfigureAwait(false);
            IAudioStreamInfo? audio = manifest.GetAudioOnlyStreams()
                .Where(stream => stream.Container == Container.Mp4 || stream.Container == Container.WebM)
                .OrderByDescending(stream => stream.IsAudioLanguageDefault)
                .ThenBy(stream => stream.Bitrate.BitsPerSecond)
                .FirstOrDefault();
            if (audio is null) {
                throw new HttpRequestException("No supported YouTube audio stream is available.");
            }
            if (audio.Size.Bytes > RecipeVideoProcessor.MaximumVideoBytes) {
                throw new InvalidDataException("Video audio exceeds the supported size limit.");
            }
            Stream input = await youtube.Videos.Streams.GetAsync(audio, cancellationToken).ConfigureAwait(false);
            await using (input.ConfigureAwait(false)) {
                await RecipeVideoProcessor.CopyBoundedAsync(input, destination, cancellationToken).ConfigureAwait(false);
            }
        } catch (YoutubeExplodeException) {
            // Provider diagnostics can contain source URLs. Keep them out of logs and API errors.
            throw new HttpRequestException("YouTube video audio is unavailable.");
        }
    }
}
