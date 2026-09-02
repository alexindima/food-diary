using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Mediator.Tests;

public sealed partial class MediatorTests {
    [Fact]
    public async Task Send_WithTypedRequest_InvokesMatchingHandler() {
        await using ServiceProvider provider = CreateProvider(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        ISender sender = provider.GetRequiredService<ISender>();

        EchoResponse response = await sender.Send(new EchoQuery("value"));

        Assert.Equal("handled:value", response.Value);
    }

    [Fact]
    public async Task Send_WithObjectRequest_ReturnsHandlerResponse() {
        await using ServiceProvider provider = CreateProvider(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        ISender sender = provider.GetRequiredService<ISender>();

        object? response = await sender.Send((object)new EchoQuery("object-value"));

        EchoResponse echoResponse = Assert.IsType<EchoResponse>(response);
        Assert.Equal("handled:object-value", echoResponse.Value);
    }

    [Fact]
    public async Task Mediator_WithObjectRequestAndNotification_UsesCombinedInterface() {
        await using ServiceProvider provider = CreateProvider(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        IMediator mediator = provider.GetRequiredService<IMediator>();
        NotificationLog.Entries.Clear();

        object? response = await mediator.Send((object)new EchoQuery("mediator-object"));
        await mediator.Publish((object)new SampleNotification("mediator-object"));

        EchoResponse echoResponse = Assert.IsType<EchoResponse>(response);
        Assert.Equal("handled:mediator-object", echoResponse.Value);
        Assert.Equal(
            ["first:mediator-object", "second:mediator-object"],
            NotificationLog.Entries,
            StringComparer.Ordinal);
    }

    [Fact]
    public async Task Send_WithObjectUnitRequest_InvokesHandlerAndReturnsUnit() {
        await using ServiceProvider provider = CreateProvider(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        ISender sender = provider.GetRequiredService<ISender>();
        UnitCommandHandler.Handled = false;

        object? response = await sender.Send((object)new UnitCommand());

        Assert.IsType<Unit>(response);
        Assert.True(UnitCommandHandler.Handled);
    }

    [Fact]
    public async Task Send_WithUnitRequest_InvokesHandlerAndReturnsTask() {
        await using ServiceProvider provider = CreateProvider(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        ISender sender = provider.GetRequiredService<ISender>();
        UnitCommandHandler.Handled = false;

        await sender.Send(new UnitCommand());

        Assert.True(UnitCommandHandler.Handled);
    }

    [Fact]
    public async Task Send_PassesCancellationTokenToHandler() {
        await using ServiceProvider provider = CreateProvider(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        ISender sender = provider.GetRequiredService<ISender>();
        using var cancellationTokenSource = new CancellationTokenSource();

        await sender.Send(new CapturingTokenQuery(), cancellationTokenSource.Token);

        Assert.Equal(cancellationTokenSource.Token, CapturingTokenHandler.CapturedToken);
    }

    [Fact]
    public async Task Send_AppliesOpenBehaviorsInRegistrationOrder() {
        await using ServiceProvider provider = CreateProvider(configuration => {
            configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly);
            configuration.AddOpenBehavior(typeof(OuterBehavior<,>));
            configuration.AddOpenBehavior(typeof(InnerBehavior<,>));
        });
        ISender sender = provider.GetRequiredService<ISender>();
        BehaviorLog.Entries.Clear();

        EchoResponse response = await sender.Send(new EchoQuery("pipeline"));

        Assert.Equal("handled:pipeline", response.Value);
        Assert.Equal(
            [
                "outer-before",
                "inner-before",
                "handler",
                "inner-after",
                "outer-after",
            ],
            BehaviorLog.Entries);
    }

    [Fact]
    public async Task Send_WhenBehaviorShortCircuits_DoesNotInvokeHandler() {
        await using ServiceProvider provider = CreateProvider(configuration => {
            configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly);
            configuration.AddOpenBehavior(typeof(ShortCircuitBehavior<,>));
        });
        ISender sender = provider.GetRequiredService<ISender>();
        BehaviorLog.Entries.Clear();

        EchoResponse response = await sender.Send(new EchoQuery("ignored"));

        Assert.Equal("short-circuited", response.Value);
        Assert.Equal(["short-circuit"], BehaviorLog.Entries);
    }

    [Fact]
    public async Task Send_WhenOpenBehaviorConstraintDoesNotMatch_SkipsBehavior() {
        await using ServiceProvider provider = CreateProvider(configuration => {
            configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly);
            configuration.AddOpenBehavior(typeof(CommandOnlyBehavior<,>));
        });
        ISender sender = provider.GetRequiredService<ISender>();
        BehaviorLog.Entries.Clear();

        EchoResponse response = await sender.Send(new EchoQuery("query"));

        Assert.Equal("handled:query", response.Value);
        Assert.Equal(["handler"], BehaviorLog.Entries);
    }

    [Fact]
    public async Task Send_WhenOpenBehaviorConstraintMatches_AppliesBehavior() {
        await using ServiceProvider provider = CreateProvider(configuration => {
            configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly);
            configuration.AddOpenBehavior(typeof(CommandOnlyBehavior<,>));
        });
        ISender sender = provider.GetRequiredService<ISender>();
        BehaviorLog.Entries.Clear();

        EchoResponse response = await sender.Send(new CommandRequest("command"));

        Assert.Equal("command:command", response.Value);
        Assert.Equal(["command-behavior-before", "command-handler", "command-behavior-after"], BehaviorLog.Entries);
    }

    [Fact]
    public async Task Send_WhenHandlerIsMissing_ThrowsInvalidOperationException() {
        await using ServiceProvider provider = CreateProvider(static _ => { });
        ISender sender = provider.GetRequiredService<ISender>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => sender.Send(new EchoQuery("missing")));
    }

    [Fact]
    public async Task Send_WithObjectThatIsNotRequest_ThrowsInvalidOperationException() {
        await using ServiceProvider provider = CreateProvider(static _ => { });
        ISender sender = provider.GetRequiredService<ISender>();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.Send(new object()));

        Assert.Contains("does not implement IRequest<TResponse>", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Send_WithNullTypedRequest_ThrowsArgumentNullException() {
        await using ServiceProvider provider = CreateProvider(static _ => { });
        ISender sender = provider.GetRequiredService<ISender>();

        await Assert.ThrowsAsync<ArgumentNullException>(() => sender.Send<EchoResponse>(null!));
    }

    [Fact]
    public async Task Send_WithNullObjectRequest_ThrowsArgumentNullException() {
        await using ServiceProvider provider = CreateProvider(static _ => { });
        ISender sender = provider.GetRequiredService<ISender>();

        await Assert.ThrowsAsync<ArgumentNullException>(() => sender.Send(null!));
    }

    [Fact]
    public async Task Send_WithExplicitInterfaceHandler_SupportsTypedAndObjectDispatch() {
        await using ServiceProvider provider = CreateProvider(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        ISender sender = provider.GetRequiredService<ISender>();

        string typedResponse = await sender.Send(new ExplicitRequest("typed"));
        object? objectResponse = await sender.Send((object)new ExplicitRequest("object"));

        Assert.Equal("explicit:typed", typedResponse);
        Assert.Equal("explicit:object", objectResponse);
    }

    [Fact]
    public async Task Send_WithConcurrentColdCacheAccess_DispatchesEveryRequest() {
        await using ServiceProvider provider = CreateProvider(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        ISender sender = provider.GetRequiredService<ISender>();

        int[] responses = await Task.WhenAll(
            Enumerable.Range(0, 100).Select(index => sender.Send(new ConcurrentRequest(index))));

        Assert.Equal(Enumerable.Range(0, 100), responses);
    }

    [Fact]
    public async Task Send_WithHandlerForMultipleRequestTypes_InvokesMatchingOverload() {
        await using ServiceProvider provider = CreateProvider(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        ISender sender = provider.GetRequiredService<ISender>();

        string firstResponse = await sender.Send(new FirstMultiRequest());
        int secondResponse = await sender.Send(new SecondMultiRequest());

        Assert.Equal("first", firstResponse);
        Assert.Equal(2, secondResponse);
    }

    [Fact]
    public async Task Send_WhenHandlerThrowsSynchronously_PropagatesOriginalException() {
        await using ServiceProvider provider = CreateProvider(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        ISender sender = provider.GetRequiredService<ISender>();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.Send(new SynchronousFailureRequest()));

        Assert.Equal("synchronous request failure", exception.Message);
    }

    [Fact]
    public async Task Send_WithMultipleResponseContracts_ThrowsInvalidOperationException() {
        await using ServiceProvider provider = CreateProvider(static _ => { });
        ISender sender = provider.GetRequiredService<ISender>();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.Send((object)new MultipleResponseRequest()));

        Assert.Contains("multiple response types", exception.Message, StringComparison.Ordinal);
    }
}
