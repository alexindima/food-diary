using FoodDiary.Modules.Ai.Infrastructure.Providers.Options;
using FoodDiary.Modules.Ai.Infrastructure.Providers.Services.Recipes;
using FoodDiary.Results;
using FoodDiary.Testing.Assertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoodDiary.Modules.Ai.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
[Collection("Recipe media")]
public sealed class RecipeVideoUploadTests {
    [Fact]
    public async Task OversizedUpload_ReturnsSizeValidationAndReleasesCapacityWithoutClosingInput() {
        using var http = new HttpClient();
        ILogger<RecipeVideoProcessor> logger = Substitute.For<ILogger<RecipeVideoProcessor>>();
        var processor = new RecipeVideoProcessor(http,
            Options.Create(new RecipeVideoOptions { FfmpegPath = "unused-size-limit-test-processor" }), logger,
            new RecipeYouTubeVideoSource(http));
        await using var input = new MemoryStream(new byte[(50 * 1024 * 1024) + 1], writable: false);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        // Three attempts exceed the two processing slots if a failed upload retains its slot.
        for (int attempt = 0; attempt < 3; attempt++) {
            input.Position = 0;
            Error error = ResultAssert.Failure(await processor.ExtractAudioAsync(input,
                sourceUrl: null, deadline.Token), "Ai.InvalidRecipeVideo");
            Assert.Multiple(
                () => Assert.Equal(ErrorKind.Validation, error.Kind),
                () => Assert.True(input.CanRead),
                () => Assert.Equal(input.Length, input.Position));
        }

        object?[][] entries = [.. logger.ReceivedCalls()
            .Where(call => string.Equals(call.GetMethodInfo().Name, nameof(ILogger.Log), StringComparison.Ordinal))
            .Select(call => call.GetArguments())];
        Assert.Equal(3, entries.Length);
        Assert.All(entries, entry => {
            var fields = Assert.IsAssignableFrom<IEnumerable<KeyValuePair<string, object?>>>(entry[2])
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
            Assert.Multiple(
                () => Assert.Equal(LogLevel.Information, entry[0]),
                () => Assert.Equal("read_upload", fields["Stage"]),
                () => Assert.Equal("media_size_limit", fields["Reason"]),
                () => Assert.Null(entry[3]));
        });
    }
}
