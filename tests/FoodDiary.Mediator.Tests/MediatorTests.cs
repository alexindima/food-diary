using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Mediator.Tests;

[ExcludeFromCodeCoverage]
public sealed class MediatorTests {
    [Fact]
    public async Task Send_WithTypedRequest_InvokesMatchingHandler() {
        await using ServiceProvider provider = CreateProvider(configuration => configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        ISender sender = provider.GetRequiredService<ISender>();

        EchoResponse response = await sender.Send(new EchoQuery("value"));

        Assert.Equal("handled:value", response.Value);
    }

    [Fact]
    public async Task Send_WithObjectRequest_ReturnsHandlerResponse() {
        await using ServiceProvider provider = CreateProvider(configuration => configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        ISender sender = provider.GetRequiredService<ISender>();

        object? response = await sender.Send((object)new EchoQuery("object-value"));

        EchoResponse echoResponse = Assert.IsType<EchoResponse>(response);
        Assert.Equal("handled:object-value", echoResponse.Value);
    }

    [Fact]
    public async Task Mediator_WithObjectRequestAndNotification_UsesCombinedInterface() {
        await using ServiceProvider provider = CreateProvider(configuration => configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        IMediator mediator = provider.GetRequiredService<IMediator>();
        NotificationLog.Entries.Clear();

        object? response = await mediator.Send((object)new EchoQuery("mediator-object"));
        await mediator.Publish((object)new SampleNotification("mediator-object"));

        EchoResponse echoResponse = Assert.IsType<EchoResponse>(response);
        Assert.Equal("handled:mediator-object", echoResponse.Value);
        Assert.Equal(["first:mediator-object", "second:mediator-object"], NotificationLog.Entries.Order(StringComparer.Ordinal), StringComparer.Ordinal);
    }

    [Fact]
    public async Task Send_WithObjectUnitRequest_InvokesHandlerAndReturnsUnit() {
        await using ServiceProvider provider = CreateProvider(configuration => configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        ISender sender = provider.GetRequiredService<ISender>();
        UnitCommandHandler.Handled = false;

        object? response = await sender.Send((object)new UnitCommand());

        Assert.IsType<Unit>(response);
        Assert.True(UnitCommandHandler.Handled);
    }

    [Fact]
    public async Task Send_WithUnitRequest_InvokesHandlerAndReturnsTask() {
        await using ServiceProvider provider = CreateProvider(configuration => configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        ISender sender = provider.GetRequiredService<ISender>();
        UnitCommandHandler.Handled = false;

        await sender.Send(new UnitCommand());

        Assert.True(UnitCommandHandler.Handled);
    }

    [Fact]
    public async Task Send_PassesCancellationTokenToHandler() {
        await using ServiceProvider provider = CreateProvider(configuration => configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
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
    public void AddOpenBehavior_WithClosedBehaviorType_ThrowsArgumentException() {
        var configuration = new MediatorServiceConfiguration();

        ArgumentException ex = Assert.Throws<ArgumentException>(() => configuration.AddOpenBehavior(typeof(ClosedBehavior)));

        Assert.Contains("Behavior type must be an open generic type definition", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Publish_WithTypedNotification_InvokesAllHandlers() {
        await using ServiceProvider provider = CreateProvider(configuration => configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        IPublisher publisher = provider.GetRequiredService<IPublisher>();
        NotificationLog.Entries.Clear();

        await publisher.Publish(new SampleNotification("typed"));

        Assert.Equal(["first:typed", "second:typed"], NotificationLog.Entries.Order(StringComparer.Ordinal), StringComparer.Ordinal);
    }

    [Fact]
    public async Task Publish_WithObjectNotification_InvokesAllHandlers() {
        await using ServiceProvider provider = CreateProvider(configuration => configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        IPublisher publisher = provider.GetRequiredService<IPublisher>();
        NotificationLog.Entries.Clear();

        await publisher.Publish((object)new SampleNotification("object"));

        Assert.Equal(["first:object", "second:object"], NotificationLog.Entries.Order(StringComparer.Ordinal), StringComparer.Ordinal);
    }

    [Fact]
    public async Task Publish_WithTypedNotification_ExecutesHandlersSequentiallyInRegistrationOrder() {
        await using ServiceProvider provider = CreateSequentialNotificationProvider();
        IPublisher publisher = provider.GetRequiredService<IPublisher>();
        NotificationExecutionProbe probe = provider.GetRequiredService<NotificationExecutionProbe>();
        using var cancellationTokenSource = new CancellationTokenSource();

        await publisher.Publish(new SequentialNotification(), cancellationTokenSource.Token);

        Assert.Equal(1, probe.MaxConcurrency);
        Assert.Equal(["first:start", "first:end", "second:start", "second:end"], probe.Entries);
        Assert.All(probe.CapturedTokens, token => Assert.Equal(cancellationTokenSource.Token, token));
    }

    [Fact]
    public async Task Publish_WithObjectNotification_ExecutesHandlersSequentiallyInRegistrationOrder() {
        await using ServiceProvider provider = CreateSequentialNotificationProvider();
        IPublisher publisher = provider.GetRequiredService<IPublisher>();
        NotificationExecutionProbe probe = provider.GetRequiredService<NotificationExecutionProbe>();

        await publisher.Publish((object)new SequentialNotification());

        Assert.Equal(1, probe.MaxConcurrency);
        Assert.Equal(["first:start", "first:end", "second:start", "second:end"], probe.Entries);
    }

    [Fact]
    public async Task Publish_WhenHandlerFails_PropagatesAndDoesNotStartLaterHandlers() {
        await using ServiceProvider provider = CreateFailingNotificationProvider();
        IPublisher publisher = provider.GetRequiredService<IPublisher>();
        NotificationFailureProbe probe = provider.GetRequiredService<NotificationFailureProbe>();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => publisher.Publish(new FailingNotification()));

        Assert.Equal("notification failed", exception.Message);
        Assert.Equal(["first", "failing"], probe.Entries);
    }

    [Fact]
    public async Task Publish_WhenNoHandlersRegistered_Completes() {
        await using ServiceProvider provider = CreateProvider(static _ => { });
        IPublisher publisher = provider.GetRequiredService<IPublisher>();

        await publisher.Publish(new UnhandledNotification());
        await publisher.Publish((object)new UnhandledNotification());
    }

    [Fact]
    public async Task Publish_WithNonNotificationObject_ThrowsInvalidOperationException() {
        await using ServiceProvider provider = CreateProvider(configuration => configuration.RegisterServicesFromAssembly(typeof(MediatorTests).Assembly));
        IPublisher publisher = provider.GetRequiredService<IPublisher>();

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => publisher.Publish(new object()));

        Assert.Contains("does not implement INotification", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Publish_WithNullTypedNotification_ThrowsArgumentNullException() {
        await using ServiceProvider provider = CreateProvider(static _ => { });
        IPublisher publisher = provider.GetRequiredService<IPublisher>();

        await Assert.ThrowsAsync<ArgumentNullException>(() => publisher.Publish<SampleNotification>(null!));
    }

    [Fact]
    public async Task Publish_WithNullObjectNotification_ThrowsArgumentNullException() {
        await using ServiceProvider provider = CreateProvider(static _ => { });
        IPublisher publisher = provider.GetRequiredService<IPublisher>();

        await Assert.ThrowsAsync<ArgumentNullException>(() => publisher.Publish(null!));
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

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sender.Send(new object()));

        Assert.Contains("does not implement IRequest<TResponse>", ex.Message, StringComparison.Ordinal);
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
    public void CreateStream_ThrowsNotSupportedException() {
        using ServiceProvider provider = CreateProvider(static _ => { });
        ISender sender = provider.GetRequiredService<ISender>();

        Assert.Throws<NotSupportedException>(() => sender.CreateStream(new SampleStreamRequest()));
        Assert.Throws<NotSupportedException>(() => sender.CreateStream((object)new SampleStreamRequest()));
    }

    private static ServiceProvider CreateProvider(Action<MediatorServiceConfiguration> configure) {
        var services = new ServiceCollection();
        services.AddFoodDiaryMediator(configure);
        return services.BuildServiceProvider();
    }

    private static ServiceProvider CreateSequentialNotificationProvider() {
        var services = new ServiceCollection();
        services.AddFoodDiaryMediator(static _ => { });
        services.AddScoped<NotificationExecutionProbe>();
        services.AddTransient<INotificationHandler<SequentialNotification>, SequentialFirstNotificationHandler>();
        services.AddTransient<INotificationHandler<SequentialNotification>, SequentialSecondNotificationHandler>();
        return services.BuildServiceProvider();
    }

    private static ServiceProvider CreateFailingNotificationProvider() {
        var services = new ServiceCollection();
        services.AddFoodDiaryMediator(static _ => { });
        services.AddScoped<NotificationFailureProbe>();
        services.AddTransient<INotificationHandler<FailingNotification>, SuccessfulNotificationHandler>();
        services.AddTransient<INotificationHandler<FailingNotification>, FailingNotificationHandler>();
        services.AddTransient<INotificationHandler<FailingNotification>, NeverStartedNotificationHandler>();
        return services.BuildServiceProvider();
    }

    [ExcludeFromCodeCoverage]
    private sealed record EchoQuery(string Value) : IRequest<EchoResponse>;

    [ExcludeFromCodeCoverage]
    private sealed record EchoResponse(string Value);

    [ExcludeFromCodeCoverage]
    private sealed class EchoQueryHandler : IRequestHandler<EchoQuery, EchoResponse> {
        public Task<EchoResponse> Handle(EchoQuery request, CancellationToken cancellationToken) {
            BehaviorLog.Entries.Add("handler");
            return Task.FromResult(new EchoResponse($"handled:{request.Value}"));
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed record UnitCommand : IRequest;

    [ExcludeFromCodeCoverage]
    private sealed class UnitCommandHandler : IRequestHandler<UnitCommand, Unit> {
        public static bool Handled { get; set; }

        public Task<Unit> Handle(UnitCommand request, CancellationToken cancellationToken) {
            Handled = true;
            return Task.FromResult(Unit.Value);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed record CapturingTokenQuery : IRequest<Unit>;

    [ExcludeFromCodeCoverage]
    private sealed class CapturingTokenHandler : IRequestHandler<CapturingTokenQuery, Unit> {
        public static CancellationToken CapturedToken { get; private set; }

        public Task<Unit> Handle(CapturingTokenQuery request, CancellationToken cancellationToken) {
            CapturedToken = cancellationToken;
            return Task.FromResult(Unit.Value);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed record CommandRequest(string Value) : ICommandRequest<EchoResponse>;

    private interface ICommandRequest<out TResponse> : IRequest<TResponse>;

    [ExcludeFromCodeCoverage]
    private sealed class CommandRequestHandler : IRequestHandler<CommandRequest, EchoResponse> {
        public Task<EchoResponse> Handle(CommandRequest request, CancellationToken cancellationToken) {
            BehaviorLog.Entries.Add("command-handler");
            return Task.FromResult(new EchoResponse($"command:{request.Value}"));
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class OuterBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull {
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken) {
            BehaviorLog.Entries.Add("outer-before");
            TResponse? response = await next(cancellationToken).ConfigureAwait(false);
            BehaviorLog.Entries.Add("outer-after");
            return response;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class InnerBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull {
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken) {
            BehaviorLog.Entries.Add("inner-before");
            TResponse? response = await next(cancellationToken).ConfigureAwait(false);
            BehaviorLog.Entries.Add("inner-after");
            return response;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class ShortCircuitBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull {
        public Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken) {
            BehaviorLog.Entries.Add("short-circuit");
            return typeof(TResponse) == typeof(EchoResponse)
                ? Task.FromResult((TResponse)(object)new EchoResponse("short-circuited"))
                : next(cancellationToken);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class CommandOnlyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : ICommandRequest<TResponse> {
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken) {
            BehaviorLog.Entries.Add("command-behavior-before");
            TResponse? response = await next(cancellationToken).ConfigureAwait(false);
            BehaviorLog.Entries.Add("command-behavior-after");
            return response;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class ClosedBehavior : IPipelineBehavior<EchoQuery, EchoResponse> {
        public Task<EchoResponse> Handle(
            EchoQuery request,
            RequestHandlerDelegate<EchoResponse> next,
            CancellationToken cancellationToken) =>
            next(cancellationToken);
    }

    [ExcludeFromCodeCoverage]
    private sealed record SampleNotification(string Value) : INotification;

    [ExcludeFromCodeCoverage]
    private sealed record UnhandledNotification : INotification;

    [ExcludeFromCodeCoverage]
    private sealed class FirstNotificationHandler : INotificationHandler<SampleNotification> {
        public Task Handle(SampleNotification notification, CancellationToken cancellationToken) {
            NotificationLog.Entries.Add($"first:{notification.Value}");
            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class SecondNotificationHandler : INotificationHandler<SampleNotification> {
        public Task Handle(SampleNotification notification, CancellationToken cancellationToken) {
            NotificationLog.Entries.Add($"second:{notification.Value}");
            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed record SequentialNotification : INotification;

    [ExcludeFromCodeCoverage]
    private sealed class SequentialFirstNotificationHandler(NotificationExecutionProbe probe)
        : INotificationHandler<SequentialNotification> {
        public Task Handle(SequentialNotification notification, CancellationToken cancellationToken) =>
            probe.ExecuteAsync("first", cancellationToken);
    }

    [ExcludeFromCodeCoverage]
    private sealed class SequentialSecondNotificationHandler(NotificationExecutionProbe probe)
        : INotificationHandler<SequentialNotification> {
        public Task Handle(SequentialNotification notification, CancellationToken cancellationToken) =>
            probe.ExecuteAsync("second", cancellationToken);
    }

    [ExcludeFromCodeCoverage]
    private sealed class NotificationExecutionProbe {
        private readonly Lock _sync = new();
        private int _activeHandlers;

        public List<string> Entries { get; } = [];

        public List<CancellationToken> CapturedTokens { get; } = [];

        public int MaxConcurrency { get; private set; }

        public async Task ExecuteAsync(string handlerName, CancellationToken cancellationToken) {
            lock (_sync) {
                _activeHandlers++;
                MaxConcurrency = Math.Max(MaxConcurrency, _activeHandlers);
                Entries.Add($"{handlerName}:start");
                CapturedTokens.Add(cancellationToken);
            }

            try {
                await Task.Delay(TimeSpan.FromMilliseconds(20), cancellationToken);
            } finally {
                lock (_sync) {
                    Entries.Add($"{handlerName}:end");
                    _activeHandlers--;
                }
            }
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed record FailingNotification : INotification;

    [ExcludeFromCodeCoverage]
    private sealed class SuccessfulNotificationHandler(NotificationFailureProbe probe)
        : INotificationHandler<FailingNotification> {
        public Task Handle(FailingNotification notification, CancellationToken cancellationToken) {
            probe.Entries.Add("first");
            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class FailingNotificationHandler(NotificationFailureProbe probe)
        : INotificationHandler<FailingNotification> {
        public Task Handle(FailingNotification notification, CancellationToken cancellationToken) {
            probe.Entries.Add("failing");
            return Task.FromException(new InvalidOperationException("notification failed"));
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class NeverStartedNotificationHandler(NotificationFailureProbe probe)
        : INotificationHandler<FailingNotification> {
        public Task Handle(FailingNotification notification, CancellationToken cancellationToken) {
            probe.Entries.Add("unexpected");
            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class NotificationFailureProbe {
        public List<string> Entries { get; } = [];
    }

    [ExcludeFromCodeCoverage]
    private sealed record SampleStreamRequest : IStreamRequest<string>;

    [ExcludeFromCodeCoverage]
    private static class BehaviorLog {
        public static List<string> Entries { get; } = [];
    }

    [ExcludeFromCodeCoverage]
    private static class NotificationLog {
        public static List<string> Entries { get; } = [];
    }
}
