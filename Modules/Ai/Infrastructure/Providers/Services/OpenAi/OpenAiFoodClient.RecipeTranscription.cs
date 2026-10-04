using System.Net.Http.Headers;
using System.Text.Json;
using FoodDiary.Integrations.Http.Http;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Results;
using Polly.CircuitBreaker;

namespace FoodDiary.Modules.Ai.Infrastructure.Providers.Services.OpenAi;

public sealed partial class OpenAiFoodClient {
    public async Task<Result<OpenAiFoodClientResponse<string>>> TranscribeRecipeAudioAsync(RecipeAudio audio, CancellationToken cancellationToken) {
        const string operation = "recipe-transcription";
        if (string.IsNullOrWhiteSpace(_options.ApiKey)) {
            return Result.Failure<OpenAiFoodClientResponse<string>>(AiErrors.OpenAiFailed("OpenAI API key is not configured."));
        }
        if (audio.Wav.Length is 0 or > 10 * 1024 * 1024 || audio.DurationSeconds is <= 0 or > 300) {
            return Result.Failure<OpenAiFoodClientResponse<string>>(AiErrors.InvalidRecipeVideo());
        }
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(_overallRequestTimeout);
        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(audio.Wav);
        file.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
        content.Add(file, "file", "audio.wav");
        content.Add(new StringContent(_options.TranscriptionModel), "model");
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/audio/transcriptions") { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        try {
            // No retry: an ambiguous transport failure must not duplicate a paid transcription.
            using HttpResponseMessage response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) {
                RecordAiRequest(operation, _options.TranscriptionModel, "failure");
                return Result.Failure<OpenAiFoodClientResponse<string>>(AiErrors.OpenAiFailed("Audio transcription failed."));
            }
            string body = await BoundedHttpContentReader.ReadAsStringAsync(response.Content, 128 * 1024, TimeSpan.FromSeconds(10), deadline.Token).ConfigureAwait(false);
            using var json = JsonDocument.Parse(body, new JsonDocumentOptions { MaxDepth = 16 });
            if (json.RootElement.ValueKind != JsonValueKind.Object || !json.RootElement.TryGetProperty("text", out JsonElement text) ||
                text.ValueKind != JsonValueKind.String || text.GetString() is not { Length: <= 8000 } transcript) {
                return Result.Failure<OpenAiFoodClientResponse<string>>(AiErrors.InvalidResponse("Invalid audio transcript returned by the provider."));
            }
            RecordAiRequest(operation, _options.TranscriptionModel, "success");
            return Result.Success(new OpenAiFoodClientResponse<string>(transcript, operation, _options.TranscriptionModel, ExtractUsage(json)));
        } catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidDataException or JsonException or BrokenCircuitException or TimeoutException) {
            RecordAiRequest(operation, _options.TranscriptionModel, "failure");
            return Result.Failure<OpenAiFoodClientResponse<string>>(AiErrors.OpenAiFailed("Audio transcription failed."));
        } catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
            return Result.Failure<OpenAiFoodClientResponse<string>>(AiErrors.OpenAiFailed("Audio transcription deadline expired."));
        }
    }
}
