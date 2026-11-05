using FoodDiary.Mediator;
using FoodDiary.Modules.Fasting.Presentation.Features.Logs;
using FoodDiary.Modules.Fasting.Presentation.Features.Logs.Requests;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Modules.Fasting.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class ClientTelemetryRouteTests {
    [Theory]
    [InlineData(null, null)]
    [InlineData(" ", null)]
    [InlineData("relative/route", "/unknown")]
    [InlineData("https://example.test/users/user%40example.test?code=private#fragment", "/users/:id")]
    [InlineData("/users/123;proof=private/meals/456", "/users/:id/meals/:id")]
    [InlineData("/meals/99887a71-c1cd-4afa-9da1-25c96f52845b/edit#session=private", "/meals/:id/edit")]
    public async Task LoggedRoute_RemovesIdentifiersCredentialsAndMatrixParameters(string? route, string? expected) {
        ISender sender = Substitute.For<ISender>();
        ILogger<LogsController> logger = Substitute.For<ILogger<LogsController>>();
        var processor = new ClientTelemetryHttpProcessor(sender, logger);
        var request = new ClientTelemetryLogHttpRequest("route_timing", "router.navigation", "info", "2026-11-02T12:00:00Z", PageRoute: route);

        await processor.ProcessAsync(request, hasAuthenticatedUser: false, CancellationToken.None);

        object? logState = Assert.Single(logger.ReceivedCalls(), call => string.Equals(call.GetMethodInfo().Name, nameof(ILogger.Log), StringComparison.Ordinal)).GetArguments()[2];
        var fields = Assert.IsAssignableFrom<IEnumerable<KeyValuePair<string, object?>>>(logState).ToDictionary(StringComparer.Ordinal);
        Assert.Equal(expected, fields["PageRoute"]);
        object? scopeState = Assert.Single(logger.ReceivedCalls(), call => string.Equals(call.GetMethodInfo().Name, nameof(ILogger.BeginScope), StringComparison.Ordinal)).GetArguments()[0];
        var scope = Assert.IsAssignableFrom<IEnumerable<KeyValuePair<string, object?>>>(scopeState).ToDictionary(StringComparer.Ordinal);
        Assert.Equal(expected, scope["ClientTelemetryPageRoute"]);
        Assert.Empty(sender.ReceivedCalls());
    }
}
