using System.Net;
using System.Text;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Infrastructure.Providers.Options;
using FoodDiary.Modules.Ai.Infrastructure.Providers.Services.OpenAi;
using FoodDiary.Testing.Assertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FoodDiary.Modules.Ai.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
[Collection("OpenAI provider")]
public sealed class RecipeTranscriptionTests {
    [Fact]
    public async Task Transcription_SendsOnlyWavAndModel_AndAcceptsDurationUsage() {
        using var handler = new AudioHandler(HttpStatusCode.OK, "{\"text\":\"180g yoghurt\",\"usage\":{\"type\":\"duration\",\"seconds\":1}}");
        using var http = new HttpClient(handler);
        OpenAiFoodClient client = CreateClient(http);
        OpenAiFoodClientResponse<string> result = ResultAssert.Success(await client.TranscribeRecipeAudioAsync(new RecipeAudio(Encoding.ASCII.GetBytes("RIFF-test-audio"), 1, "https://example.org/private-source"), CancellationToken.None));
        Assert.Multiple(
            () => Assert.Equal("https://api.openai.com/v1/audio/transcriptions", handler.Url),
            () => Assert.Contains("audio/wav", handler.Body, StringComparison.Ordinal),
            () => Assert.Contains("audio.wav", handler.Body, StringComparison.Ordinal),
            () => Assert.Contains("gpt-transcribe", handler.Body, StringComparison.Ordinal),
            () => Assert.DoesNotContain("private-source", handler.Body, StringComparison.Ordinal),
            () => Assert.Equal("180g yoghurt", result.Value),
            () => Assert.Null(result.Usage));
    }

    [Fact]
    public async Task Transcription_ProviderFailure_IsNotRetried() {
        using var handler = new AudioHandler(HttpStatusCode.ServiceUnavailable, "provider-error");
        using var http = new HttpClient(handler);
        ResultAssert.Failure(await CreateClient(http).TranscribeRecipeAudioAsync(new RecipeAudio([1], 1, SourceUrl: null), CancellationToken.None), "Ai.OpenAiFailed");
        Assert.Equal(1, handler.Calls);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"text\":null}")]
    [InlineData("not JSON")]
    public async Task Transcription_MalformedResponse_ReturnsControlledFailure(string response) {
        using var handler = new AudioHandler(HttpStatusCode.OK, response);
        using var http = new HttpClient(handler);
        ResultAssert.Failure(await CreateClient(http).TranscribeRecipeAudioAsync(new RecipeAudio([1], 1, SourceUrl: null), CancellationToken.None));
    }

    private static OpenAiFoodClient CreateClient(HttpClient http) => new(http, Options.Create(new OpenAiOptions { ApiKey = "unit-test-key" }), NullLogger<OpenAiFoodClient>.Instance);

    [ExcludeFromCodeCoverage]
    private sealed class AudioHandler(HttpStatusCode status, string response) : HttpMessageHandler {
        public string? Body { get; private set; }
        public string? Url { get; private set; }
        public int Calls { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            Calls++;
            Url = request.RequestUri!.AbsoluteUri;
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status) { Content = new StringContent(response, Encoding.UTF8, "application/json") };
        }
    }
}
