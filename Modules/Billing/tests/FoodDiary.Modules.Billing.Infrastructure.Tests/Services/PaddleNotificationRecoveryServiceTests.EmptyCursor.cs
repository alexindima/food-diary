using FoodDiary.Modules.Billing.Infrastructure.Providers.Billing;
using MsOptions = Microsoft.Extensions.Options.Options;
using System.Text.Json;
using FoodDiary.Modules.Billing.Contracts.Models;

namespace FoodDiary.Modules.Billing.Infrastructure.Tests.Services;

public sealed partial class PaddleNotificationRecoveryServiceTests {
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task MissingNextCursor_StopsDespiteTheProvidersHasMoreFlag(string? next) {
        string body = JsonSerializer.Serialize(new { data = Array.Empty<object>(), meta = new { pagination = new { has_more = true, next } } });
        var handler = new QueueHandler(JsonResponse(body));
        var service = new PaddleNotificationRecoveryService(new HttpClient(handler), MsOptions.Create(ValidOptions()));

        PaddleNotificationRecoveryResult result = await service.ReplayFailedAsync(CancellationToken.None);

        Assert.False(result.WasLimited);
        Assert.Equal(0, result.Replayed);
        Assert.Single(handler.Requests);
    }
}
