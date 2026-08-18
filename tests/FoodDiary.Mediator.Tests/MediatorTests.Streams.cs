using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Mediator.Tests;

public sealed partial class MediatorTests {
    [Fact]
    public async Task CreateStream_WithTypedAndObjectRequests_ReturnsHandlerResponses() {
        await using ServiceProvider provider = CreateProvider(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        ISender sender = provider.GetRequiredService<ISender>();
        using var cancellationTokenSource = new CancellationTokenSource();

        List<string> typedResponses = [];
        await foreach (string response in sender.CreateStream(
            new SampleStreamRequest(2),
            cancellationTokenSource.Token)) {
            typedResponses.Add(response);
        }

        List<object?> objectResponses = [];
        await foreach (object? response in sender.CreateStream(
            (object)new SampleStreamRequest(2),
            cancellationTokenSource.Token)) {
            objectResponses.Add(response);
        }

        Assert.Equal(["item-0", "item-1"], typedResponses);
        Assert.Equal(["item-0", "item-1"], objectResponses);
        Assert.Equal(cancellationTokenSource.Token, SampleStreamRequestHandler.CapturedToken);
    }

    [Fact]
    public async Task CreateStream_WithCancelledToken_PropagatesCancellation() {
        await using ServiceProvider provider = CreateProvider(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        ISender sender = provider.GetRequiredService<ISender>();
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => {
            await foreach (string _ in sender.CreateStream(
                new SampleStreamRequest(1),
                cancellationTokenSource.Token)) {
            }
        });
    }

    [Fact]
    public async Task CreateStream_WhenHandlerIsMissing_ThrowsInvalidOperationExceptionOnEnumeration() {
        await using ServiceProvider provider = CreateProvider(static _ => { });
        ISender sender = provider.GetRequiredService<ISender>();

        await Assert.ThrowsAsync<InvalidOperationException>(async () => {
            await foreach (string _ in sender.CreateStream(new SampleStreamRequest(1))) {
            }
        });
    }

    [Fact]
    public void CreateStream_WithInvalidOrAmbiguousObject_ThrowsInvalidOperationException() {
        using ServiceProvider provider = CreateProvider(static _ => { });
        ISender sender = provider.GetRequiredService<ISender>();

        InvalidOperationException invalidException = Assert.Throws<InvalidOperationException>(
            () => sender.CreateStream(new object()));
        InvalidOperationException ambiguousException = Assert.Throws<InvalidOperationException>(
            () => sender.CreateStream((object)new MultipleResponseStreamRequest()));

        Assert.Contains("does not implement IStreamRequest<TResponse>", invalidException.Message, StringComparison.Ordinal);
        Assert.Contains("multiple response types", ambiguousException.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateStream_WithNullRequests_ThrowsArgumentNullException() {
        using ServiceProvider provider = CreateProvider(static _ => { });
        ISender sender = provider.GetRequiredService<ISender>();

        Assert.Throws<ArgumentNullException>(() => sender.CreateStream<string>(null!));
        Assert.Throws<ArgumentNullException>(() => sender.CreateStream(null!));
    }

    [Fact]
    public async Task CreateStream_WithExplicitInterfaceHandler_SupportsTypedAndObjectDispatch() {
        await using ServiceProvider provider = CreateProvider(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        ISender sender = provider.GetRequiredService<ISender>();

        var typedResponses = new List<string>();
        await foreach (string response in sender.CreateStream(new ExplicitStreamRequest("typed"))) {
            typedResponses.Add(response);
        }

        var objectResponses = new List<object?>();
        await foreach (object? response in sender.CreateStream((object)new ExplicitStreamRequest("object"))) {
            objectResponses.Add(response);
        }

        Assert.Equal(["explicit:typed"], typedResponses);
        Assert.Equal(["explicit:object"], objectResponses);
    }

    [Fact]
    public async Task CreateStream_WithHandlersRegisteredAfterMediator_RejectsAmbiguousRuntimeResolution() {
        var services = new ServiceCollection();
        services.AddFoodDiaryMediator(static _ => { });
        services.AddTransient<IStreamRequestHandler<DuplicateStreamRequest, string>, DuplicateStreamRequestHandler>();
        services.AddTransient<IStreamRequestHandler<DuplicateStreamRequest, string>, DuplicateStreamRequestHandler>();
        await using ServiceProvider provider = services.BuildServiceProvider();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => {
            await foreach (string _ in provider
                .GetRequiredService<ISender>()
                .CreateStream(new DuplicateStreamRequest())) {
            }
        });

        Assert.Contains("Multiple mediator handlers", exception.Message, StringComparison.Ordinal);
    }
}
