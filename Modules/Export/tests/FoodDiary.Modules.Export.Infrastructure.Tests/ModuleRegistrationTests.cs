using System.Net;
using System.Net.Sockets;
using System.Reflection;
using FoodDiary.Application.Abstractions.Export.Common;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Export.Infrastructure.Tests;

[ExcludeFromCodeCoverage]
public sealed class ModuleRegistrationTests {
    [Fact]
    public void AddExportInfrastructure_PreservesHostTimeProviderAndSafeHttpHandler() {
        var services = new ServiceCollection();
        var timeProvider = new TestTimeProvider();
        services.AddSingleton<TimeProvider>(timeProvider);
        services.AddExportInfrastructure();
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Same(timeProvider, provider.GetRequiredService<TimeProvider>());
        using HttpClient client = provider.GetRequiredService<IHttpClientFactory>()
            .CreateClient(nameof(IDiaryPdfGenerator));
        Assert.Equal(TimeSpan.FromSeconds(5), client.Timeout);
        HttpMessageHandler handler = provider.GetRequiredService<IHttpMessageHandlerFactory>()
            .CreateHandler(nameof(IDiaryPdfGenerator));
        while (handler is DelegatingHandler delegatingHandler) {
            handler = Assert.IsAssignableFrom<HttpMessageHandler>(delegatingHandler.InnerHandler);
        }

        SocketsHttpHandler sockets = Assert.IsType<SocketsHttpHandler>(handler);
        Assert.Multiple(
            () => Assert.False(sockets.AllowAutoRedirect),
            () => Assert.False(sockets.UseProxy),
            () => Assert.NotNull(sockets.ConnectCallback));
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestTimeProvider : TimeProvider;

    [Fact]
    public void AddExportInfrastructure_CanResolveDiaryPdfGeneratorTypedClient() {
        var services = new ServiceCollection();
        services.AddSingleton<IDiaryPdfReportTextProvider, TestDiaryPdfReportTextProvider>();
        Assert.Same(services, services.AddExportInfrastructure());
        using ServiceProvider provider = services.BuildServiceProvider();

        IDiaryPdfGenerator generator = provider.GetRequiredService<IDiaryPdfGenerator>();

        Assert.NotNull(generator);
    }

    [Fact]
    public async Task ConnectToAllowedRemoteImageEndpointAsync_WhenHostResolvesToLoopback_RejectsConnection() {
        SocketsHttpConnectionContext context = CreateSocketsHttpConnectionContext(
            new DnsEndPoint("localhost", 80),
            new HttpRequestMessage(HttpMethod.Get, "http://localhost/"));

        HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(async () => {
            await InvokePrivateStatic<ValueTask<Stream>>(
                "ConnectToAllowedRemoteImageEndpointAsync",
                context,
                CancellationToken.None).ConfigureAwait(true);
        }).ConfigureAwait(true);

        Assert.Contains("private or loopback", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConnectToAllowedRemoteImageEndpointAsync_WhenHostResolvesToPublicAddress_UsesSocketConnector() {
        Func<string, CancellationToken, ValueTask<IPAddress[]>> originalResolver =
            ModuleRegistration.ResolveRemoteImageHostAddressesAsync;
        Func<IPAddress, int, CancellationToken, ValueTask<Stream>> originalConnector =
            ModuleRegistration.ConnectRemoteImageSocketAsync;
        try {
            ModuleRegistration.ResolveRemoteImageHostAddressesAsync =
                static (_, _) => ValueTask.FromResult<IPAddress[]>([IPAddress.Parse("8.8.8.8")]);
            ModuleRegistration.ConnectRemoteImageSocketAsync =
                static (address, port, _) => {
                    Assert.Equal(IPAddress.Parse("8.8.8.8"), address);
                    Assert.Equal(443, port);
                    return ValueTask.FromResult<Stream>(new MemoryStream([1, 2, 3]));
                };
            SocketsHttpConnectionContext context = CreateSocketsHttpConnectionContext(
                new DnsEndPoint("push.example.com", 443),
                new HttpRequestMessage(HttpMethod.Get, "https://push.example.com/"));

            await using Stream stream = await InvokePrivateStatic<ValueTask<Stream>>(
                "ConnectToAllowedRemoteImageEndpointAsync",
                context,
                CancellationToken.None);

            Assert.Equal(3, stream.Length);
        } finally {
            ModuleRegistration.ResolveRemoteImageHostAddressesAsync = originalResolver;
            ModuleRegistration.ConnectRemoteImageSocketAsync = originalConnector;
        }
    }

    [Theory]
    [InlineData("192.0.0.1")]
    [InlineData("198.18.0.1")]
    public async Task ConnectToAllowedRemoteImageEndpointAsync_WhenReboundToSpecialUseAddress_RejectsConnection(string address) {
        Func<string, CancellationToken, ValueTask<IPAddress[]>> originalResolver =
            ModuleRegistration.ResolveRemoteImageHostAddressesAsync;
        Func<IPAddress, int, CancellationToken, ValueTask<Stream>> originalConnector =
            ModuleRegistration.ConnectRemoteImageSocketAsync;
        bool connectorCalled = false;
        try {
            ModuleRegistration.ResolveRemoteImageHostAddressesAsync =
                (_, _) => ValueTask.FromResult<IPAddress[]>([IPAddress.Parse(address)]);
            ModuleRegistration.ConnectRemoteImageSocketAsync = (_, _, _) => {
                connectorCalled = true;
                return ValueTask.FromResult<Stream>(new MemoryStream());
            };
            SocketsHttpConnectionContext context = CreateSocketsHttpConnectionContext(
                new DnsEndPoint("rebound.example.com", 443),
                new HttpRequestMessage(HttpMethod.Get, "https://rebound.example.com/image.png"));

            await Assert.ThrowsAsync<HttpRequestException>(async () => {
                await InvokePrivateStatic<ValueTask<Stream>>(
                    "ConnectToAllowedRemoteImageEndpointAsync",
                    context,
                    CancellationToken.None).ConfigureAwait(true);
            }).ConfigureAwait(true);

            Assert.False(connectorCalled);
        } finally {
            ModuleRegistration.ResolveRemoteImageHostAddressesAsync = originalResolver;
            ModuleRegistration.ConnectRemoteImageSocketAsync = originalConnector;
        }
    }

    [Fact]
    public async Task ConnectRemoteImageSocketAsync_WhenListenerAcceptsConnection_ReturnsNetworkStream() {
        var listener = new TcpListener(IPAddress.Loopback, port: 0);
        listener.Start();
        try {
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            ValueTask<Stream> streamTask = ModuleRegistration.ConnectRemoteImageSocketAsync(
                IPAddress.Loopback,
                port,
                CancellationToken.None);
            using TcpClient client = await listener.AcceptTcpClientAsync();
            await using Stream stream = await streamTask;

            Assert.True(stream.CanRead);
        } finally {
            listener.Stop();
        }
    }

    [Fact]
    public async Task ConnectRemoteImageSocketAsync_WhenConnectionFails_DisposesSocketAndThrows() {
        var listener = new TcpListener(IPAddress.Loopback, port: 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await Assert.ThrowsAnyAsync<SocketException>(async () => {
            await ModuleRegistration.ConnectRemoteImageSocketAsync(
                IPAddress.Loopback,
                port,
                cancellationTokenSource.Token).ConfigureAwait(false);
        });
    }

    [Theory]
    [InlineData("8.8.8.8", true)]
    [InlineData("127.0.0.1", false)]
    [InlineData("0.0.0.0", false)]
    [InlineData("255.255.255.255", false)]
    [InlineData("10.1.2.3", false)]
    [InlineData("172.16.0.1", false)]
    [InlineData("172.31.255.255", false)]
    [InlineData("172.32.0.1", true)]
    [InlineData("192.168.1.1", false)]
    [InlineData("192.0.0.1", false)]
    [InlineData("198.18.0.1", false)]
    [InlineData("198.19.255.255", false)]
    [InlineData("169.254.1.1", false)]
    [InlineData("100.64.0.1", false)]
    [InlineData("100.127.255.255", false)]
    [InlineData("100.128.0.1", true)]
    [InlineData("224.0.0.1", false)]
    [InlineData("::1", false)]
    [InlineData("::", false)]
    [InlineData("2001:4860:4860::8888", true)]
    [InlineData("fe80::1", false)]
    [InlineData("fec0::1", false)]
    [InlineData("fc00::1", false)]
    [InlineData("ff02::1", false)]
    [InlineData("::ffff:8.8.8.8", true)]
    [InlineData("::ffff:10.1.2.3", false)]
    public void IsPublicAddress_ReturnsExpectedResult(string address, bool expected) {
        bool result = InvokePrivateStatic<bool>("IsPublicAddress", IPAddress.Parse(address));

        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsPublicAddressCore_WhenAddressFamilyIsUnsupported_ReturnsFalse() {
        bool result = ModuleRegistration.IsPublicAddressCore(
            AddressFamily.Unknown,
            [1, 2, 3, 4],
            isIPv6LinkLocal: false,
            isIPv6SiteLocal: false,
            isIPv6Multicast: false);

        Assert.False(result);
    }

    private static T InvokePrivateStatic<T>(string methodName, params object[] args) {
        MethodInfo method = typeof(ModuleRegistration).GetMethod(
            methodName,
            BindingFlags.Static | BindingFlags.NonPublic)!;
        return (T)method.Invoke(null, args)!;
    }

    private static SocketsHttpConnectionContext CreateSocketsHttpConnectionContext(
        DnsEndPoint dnsEndPoint,
        HttpRequestMessage request) {
        ConstructorInfo constructor = typeof(SocketsHttpConnectionContext).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            [typeof(DnsEndPoint), typeof(HttpRequestMessage)],
            modifiers: null)!;

        return (SocketsHttpConnectionContext)constructor.Invoke([dnsEndPoint, request]);
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestDiaryPdfReportTextProvider : IDiaryPdfReportTextProvider {
        public DiaryPdfReportTexts GetTexts(string? locale) =>
            new(
                CultureName: "en",
                ReportTitle: "Food Diary Report",
                PeriodLabel: "Period",
                MealsCountLabel: "{0} meals",
                PeriodSummaryTitle: "Period summary",
                TotalCaloriesTitle: "Total calories",
                KcalUnit: "kcal",
                AveragePerDayTitle: "Average per day",
                TotalForPeriodTitle: "Total for period",
                ProteinsTitle: "Proteins",
                FatsTitle: "Fats",
                CarbsTitle: "Carbs",
                FiberTitle: "Fiber",
                GramsUnit: "g",
                GramsProteinsLabel: "g proteins",
                GramsFatsLabel: "g fats",
                GramsCarbsLabel: "g carbs",
                GramsFiberLabel: "g fiber",
                CaloriesByDayTitle: "Calories by day",
                NutrientsByDayTitle: "Nutrients by day",
                MealsTitle: "Meals",
                NoMealsMessage: "No meals recorded in this period.",
                DateColumn: "Date",
                TypeColumn: "Type",
                ItemsColumn: "Items",
                AmountColumn: "Amount",
                KcalColumn: "Kcal",
                ProteinsColumnShort: "Proteins, g",
                FatsColumnShort: "Fats, g",
                CarbsColumnShort: "Carbs, g",
                FiberColumnShort: "Fiber, g",
                SatietyColumn: "Satiety",
                CommentColumn: "Comment",
                BeforeLabel: "Hunger before",
                AfterLabel: "Satiety after",
                OtherMealType: "Other",
                BreakfastMealType: "Breakfast",
                LunchMealType: "Lunch",
                DinnerMealType: "Dinner",
                SnackMealType: "Snack",
                ItemsPrefix: "Items",
                ItemsNotSpecified: "not specified",
                MoreItemsSuffix: "more",
                RecipeFallback: "Recipe",
                ProductFallback: "Product",
                ServingUnit: "serv.",
                GeneratedByPrefix: "Generated by Food Diary - ");
    }

}
