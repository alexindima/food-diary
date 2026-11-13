using System.Net;
using FoodDiary.Mediator;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Application.Commands.ProcessTelegramStarsUpdate;
using FoodDiary.Modules.Billing.Presentation.Requests;
using FoodDiary.Modules.Billing.Presentation.Responses;
using FoodDiary.Results;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class TelegramStarsTransportIntegrationTests(TransportApiWebApplicationFactory factory)
    : IClassFixture<TransportApiWebApplicationFactory> {
    [Theory]
    [InlineData(null)]
    [InlineData("wrong")]
    public async Task StarsUpdate_RequiresBotSecretBeforeDispatchAsync(string? provided) {
        ISender sender = Substitute.For<ISender>();
        await using WebApplicationFactory<Program> configured = CreateFactory(sender);
        using HttpClient client = configured.CreateClient();
        if (provided is not null) {
            client.DefaultRequestHeaders.Add("X-Telegram-Bot-Secret", provided);
        }
        using HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/billing/telegram-stars/updates", Request());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await sender.DidNotReceive().Send(Arg.Any<ProcessTelegramStarsUpdateCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StarsUpdate_MapsAuthenticatedEnvelopeAndCheckoutDecisionAsync() {
        ISender sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<ProcessTelegramStarsUpdateCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new TelegramStarsUpdateResultModel(CheckoutApproved: true)));
        await using WebApplicationFactory<Program> configured = CreateFactory(sender);
        using HttpClient client = configured.CreateClient();
        client.DefaultRequestHeaders.Add("X-Telegram-Bot-Secret", new string('s', 32));
        using HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/billing/telegram-stars/updates", Request());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        TelegramStarsUpdateHttpResponse? body = await response.Content.ReadFromJsonAsync<TelegramStarsUpdateHttpResponse>();
        Assert.NotNull(body);
        Assert.True(body.CheckoutApproved);
        await sender.Received(1).Send(Arg.Is<ProcessTelegramStarsUpdateCommand>(command =>
            command.Update.UpdateId == 10 && command.Update.TelegramUserId == 123 &&
            command.Update.TotalAmount == 500 && string.Equals(command.Update.InvoicePayload, "invoice_owned", StringComparison.Ordinal)), Arg.Any<CancellationToken>());
    }

    private WebApplicationFactory<Program> CreateFactory(ISender sender) =>
        factory.WithWebHostBuilder(builder => {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["TelegramBot:ApiSecret"] = new string('s', 32),
            }));
            builder.ConfigureServices(services => {
                services.RemoveAll<ISender>();
                services.AddSingleton(sender);
            });
        });

    private static TelegramStarsUpdateHttpRequest Request() => new(10, 123, "pre_checkout", "invoice_owned", "XTR", 500);
}
