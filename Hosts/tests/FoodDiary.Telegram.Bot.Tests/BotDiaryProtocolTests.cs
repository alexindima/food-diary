using System.Net;
using System.Net.Http.Json;
using System.Text;
using FoodDiary.Telegram.Bot.Operations;
using Microsoft.Extensions.Options;

namespace FoodDiary.Telegram.Bot.Tests;

[ExcludeFromCodeCoverage]
public sealed class BotDiaryProtocolTests {
    [Theory]
    [InlineData("{}")]
    [InlineData("{\"accessToken\":null,\"user\":{\"id\":\"11111111-1111-1111-1111-111111111111\"}}")]
    [InlineData("{\"accessToken\":\"broken\",\"user\":null}")]
    public async Task AuthenticateAsync_RejectsIncompleteReplyAsPermanentInvalidData(string body) {
        using var handler = new Handler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        using var http = new HttpClient(handler);
        await Assert.ThrowsAsync<InvalidDataException>(() => Client(http).AuthenticateAsync(123, Lease(), CancellationToken.None));
    }

    [Theory]
    [InlineData("not-a-token")]
    [InlineData("a.%.c")]
    [InlineData("a.eA.c")]
    [InlineData("a.W10.c")]
    [InlineData("a.bnVsbA.c")]
    [InlineData("a.e30.c")]
    [InlineData("a.eyJzZWN1cml0eV92ZXJzaW9uIjoiMiJ9.c")]
    public async Task AuthenticateAsync_RejectsMalformedOrStaleSecurityVersion(string token) {
        using var handler = new Handler(_ => Json(new { AccessToken = token, User = new { Id = Lease().UserId } }));
        using var http = new HttpClient(handler);
        await Assert.ThrowsAsync<InvalidDataException>(() => Client(http).AuthenticateAsync(123, Lease(), CancellationToken.None));
    }

    [Theory]
    [InlineData("image/png", "png")]
    [InlineData("image/webp", "webp")]
    [InlineData("image/jpeg", "jpg")]
    public async Task RequestUploadAsync_UsesStableAttemptKeyAndCorrectExtension(string mime, string extension) {
        var operation = Guid.NewGuid();
        var receipt = new BotImageUpload("https://storage.example/upload", "food", DateTime.UtcNow.AddMinutes(1), Guid.NewGuid());
        using var handler = new Handler(request => {
            Assert.Equal($"telegram:{operation:N}:upload:2", Assert.Single(request.Headers.GetValues("Idempotency-Key")));
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            return Json(receipt);
        });
        using var http = new HttpClient(handler);
        Assert.Equal(receipt, await Client(http).RequestUploadAsync("token", operation, 2, mime, 3, CancellationToken.None));
        Assert.Contains($"{operation:N}.{extension}", handler.Body, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("http://storage.example/upload")]
    [InlineData("/relative")]
    [InlineData("https://user:password@storage.example/upload")]
    [InlineData("https://storage.example/upload#fragment")]
    public async Task UploadAsync_RejectsUnsafeUrlsBeforeSendingBytes(string url) {
        using var handler = new Handler(_ => throw new InvalidOperationException("No request should be sent"));
        using var http = new HttpClient(handler);
        await Assert.ThrowsAsync<InvalidDataException>(() => Client(http).UploadAsync(
            new BotImageUpload(url, "food", DateTime.UtcNow, Guid.NewGuid()), "image/jpeg", [255, 216, 255], CancellationToken.None));
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, "{\"status\":\"Undone\"}", "Undone")]
    [InlineData(HttpStatusCode.Conflict, "null", "Meal.RecognitionOperationNotFound")]
    [InlineData(HttpStatusCode.NotFound, "{\"error\":\"Missing\"}", "Missing")]
    public async Task UndoAsync_PreservesSuccessAndExpectedConflicts(HttpStatusCode status, string body, string expected) {
        using var handler = new Handler(_ => new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        using var http = new HttpClient(handler);
        Assert.Equal(expected, await Client(http).UndoRecognizedMealAsync("token", Guid.NewGuid(), CancellationToken.None));
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public async Task NonJsonGatewayError_RetainsHttpStatusForRetryPolicy(HttpStatusCode status) {
        using var handler = new Handler(_ => new HttpResponseMessage(status) { Content = new StringContent("<html>Gateway error</html>") });
        using var http = new HttpClient(handler);
        HttpRequestException error = await Assert.ThrowsAsync<HttpRequestException>(() =>
            Client(http).StartRecognitionAsync("token", Guid.NewGuid(), Guid.NewGuid(), caption: null, CancellationToken.None));
        Assert.Equal(status, error.StatusCode);
    }

    [Fact]
    public async Task EmptySuccessResponse_IsNotTreatedAsARecognition() {
        using var handler = new Handler(_ => Json<object?>(body: null));
        using var http = new HttpClient(handler);
        await Assert.ThrowsAsync<InvalidDataException>(() => Client(http).GetRecognitionAsync("token", Guid.NewGuid(), CancellationToken.None));
        await Assert.ThrowsAsync<InvalidDataException>(() => Client(http).UndoRecognizedMealAsync("token", Guid.NewGuid(), CancellationToken.None));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public async Task Statistics_RejectsUnsupportedPeriodBeforeCallingApi(int days) {
        using var handler = new Handler(_ => throw new InvalidOperationException("No request should be sent"));
        using var http = new HttpClient(handler);
        await Assert.ThrowsAsync<InvalidDataException>(() => Client(http).GetStatisticsAsync("token", days, CancellationToken.None));
    }

    [Fact]
    public async Task Statistics_RejectsSummaryForADifferentPeriod() {
        using var handler = new Handler(_ => Json(new { calendarDays = 7, days = Array.Empty<object>() }));
        using var http = new HttpClient(handler);
        await Assert.ThrowsAsync<InvalidDataException>(() => Client(http).GetStatisticsAsync("token", 1, CancellationToken.None));
    }

    [Fact]
    public async Task MealAndWater_RejectMismatchedReceipts() {
        using var handler = new Handler(_ => Json(new { operationId = Guid.NewGuid(), mealId = Guid.NewGuid(), entryId = Guid.NewGuid() }));
        using var http = new HttpClient(handler);
        await Assert.ThrowsAsync<InvalidDataException>(() => Client(http).SaveRecognizedMealAsync("token", Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidDataException>(() => Client(http).SaveWaterAsync("token", Guid.NewGuid(), DateTime.UtcNow, 250, CancellationToken.None));
    }

    [Fact]
    public async Task MissingApiConfiguration_FailsWithoutSendingRequest() {
        using var http = new HttpClient();
        var client = new BotDiaryClient(http, Options.Create(new TelegramBotOptions { ApiBaseUrl = "" }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetRecognitionAsync("token", Guid.NewGuid(), CancellationToken.None));
    }

    private static BotDiaryClient Client(HttpClient http) => new(http, Options.Create(new TelegramBotOptions { ApiBaseUrl = "https://diary.example", ApiSecret = "test-secret" }));
    private static BotOperationLease Lease() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.Parse("11111111-1111-1111-1111-111111111111"), 1, "payload", Checkpoint: null, DateTime.UtcNow);
    private static HttpResponseMessage Json<T>(T body) => new(HttpStatusCode.OK) { Content = JsonContent.Create(body) };

    [ExcludeFromCodeCoverage]
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler {
        public string Body { get; private set; } = "";
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            Body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            return respond(request);
        }
    }
}
